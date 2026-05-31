using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BookBlossom.Core.DTOs;
using BookBlossom.Core.DTOs.CheckoutAndCreateOrder;
using BookBlossom.Core.Entities;
using BookBlossom.Core.Enums;
using BookBlossom.Core.Interfaces;
using BookBlossom.Core.Interfaces.Services;
using BookBlossom.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace BookBlossom.Infrastructure.Services
{
    public class OrderService : IOrderService
    {
        private readonly ApplicationDbContext _context;
        private readonly INotificationService _notificationService;
        private readonly IGamificationService _gamificationService;
        private readonly IVoucherService _voucherService;

        static OrderService()
        {
            // Thiết lập License cho QuestPDF để có thể render PDF (bản Community)
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public OrderService(ApplicationDbContext context, INotificationService notificationService, IGamificationService gamificationService, IVoucherService voucherService)
        {
            _context = context;
            _notificationService = notificationService;
            _gamificationService = gamificationService;
            _voucherService = voucherService;
        }

        public async Task<OrderResponseDTO> CreateOrderAsync(long customerId, CheckoutRequestDTO request)
        {
            if (request.CartItems == null || !request.CartItems.Any())
            {
                throw new ArgumentException("Giỏ hàng trống, không thể tiến hành đặt hàng.");
            }

            // 1. Khởi tạo Database Transaction để đảm bảo tính toàn vẹn dữ liệu
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 2. Kiểm tra điểm uy tín từ bảng [Rank].[CustomerReputation]
                var customerRep = await _context.Set<CustomerReputation>()
                    .FirstOrDefaultAsync(cr => cr.CustomerID == customerId);

                int currentReputation = customerRep?.ReputationPoint ?? 100;

                if (currentReputation < 60 && request.PaymentMethod == PaymentMethod.COD)
                {
                    throw new InvalidOperationException($"Điểm uy tín của bạn hiện tại là ({currentReputation}), không đủ điều kiện (tối thiểu 60) để dùng hình thức COD. Vui lòng thanh toán trực tuyến!");
                }

                // 3. XỬ LÝ ĐỊA CHỈ TINH GỌN (Chuẩn theo UI Shopee/Luminae)
                var deliveryAddress = await _context.Set<DeliveryAddress>()
                    .FirstOrDefaultAsync(da => da.AddressID == request.AddressID && da.CustomerID == customerId);

                if (deliveryAddress == null)
                {
                    throw new KeyNotFoundException("Địa chỉ nhận hàng không tồn tại hoặc không thuộc quyền sở hữu của bạn.");
                }

                // 4. Khởi tạo thực thể Order với thông tin bóc tách trực tiếp từ DB
                var order = new Order
                {
                    CustomerID = customerId,
                    AddressID = deliveryAddress.AddressID, 
                    OrderDate = DateTime.UtcNow,
                    OrderStatus = OrderStatus.Pending,
                    PaymentMethod = request.PaymentMethod,
                    PaymentStatus = 0, // 0: Chưa thanh toán
                    
                    ShipReceiverName = deliveryAddress.ReceiverName,
                    ShipPhoneNumber = deliveryAddress.PhoneNumber,
                    ShipDetailAddress = deliveryAddress.DetailAddress,
                    
                    Note = null,
                    ShippingFee = 30000, // Phí vận chuyển mặc định
                    DiscountAmount = 0
                };

                decimal subTotal = 0;

                // 5. Duyệt qua từng item để kiểm tra kho kép & trừ kho vật lý
                foreach (var item in request.CartItems)
                {
                    // Trường hợp: Mua Sách Mù
                    if (item.BlindBookID.HasValue)
                    {
                        var blindBook = await _context.Set<BlindBook>()
                            .Include(b => b.RealBook) // Include để lấy luôn sách thật liên kết
                            .FirstOrDefaultAsync(b => b.BlindBookID == item.BlindBookID);

                        if (blindBook == null) throw new KeyNotFoundException($"Gói sách mù ID {item.BlindBookID} không tồn tại.");

                        if (blindBook.StockQuantity < item.Quantity)
                        {
                            throw new InvalidOperationException($"Số lượng gói Sách Mù hiện không đủ (Chỉ còn {blindBook.StockQuantity} gói).");
                        }

                        blindBook.StockQuantity -= item.Quantity;

                        if (blindBook.RealBook != null)
                        {
                            if (blindBook.RealBook.UnitsInStock < item.Quantity)
                            {
                                throw new InvalidOperationException($"Không đủ hàng trong kho vật lý cho sách thật bên trong gói.");
                            }
                            blindBook.RealBook.UnitsInStock -= item.Quantity;
                            blindBook.RealBook.ReservedQuantity += item.Quantity;
                        }

                        decimal unitPrice = blindBook.Price;
                        subTotal += unitPrice * item.Quantity;

                        order.OrderDetails.Add(new OrderDetail
                        {
                            BookID = blindBook.RealBookID, 
                            BlindBookID = blindBook.BlindBookID, 
                            UnitPrice = unitPrice,
                            Quantity = item.Quantity,
                            Discount = 0
                        });
                    }
                    // Trường hợp: Mua Sách Thường
                    else if (item.BookID.HasValue)
                    {
                        var realBook = await _context.Set<RealBook>()
                            .FirstOrDefaultAsync(b => b.BookID == item.BookID.Value);

                        if (realBook == null) throw new KeyNotFoundException($"Sách thật ID {item.BookID} không tồn tại.");

                        if (realBook.UnitsInStock < item.Quantity)
                        {
                            throw new InvalidOperationException($"Sách '{realBook.Title}' không đủ số lượng.");
                        }

                        realBook.UnitsInStock -= item.Quantity;
                        realBook.ReservedQuantity += item.Quantity;

                        decimal unitPrice = realBook.Price;
                        subTotal += unitPrice * item.Quantity;

                        order.OrderDetails.Add(new OrderDetail
                        {
                            BookID = item.BookID.Value, 
                            BlindBookID = null,
                            UnitPrice = unitPrice,
                            Quantity = item.Quantity,
                            Discount = 0
                        });
                    }
                }

                // ─── VOUCHER: Validate & Áp giảm giá ────────────────────────
                decimal discountAmount = 0;
                long? appliedVoucherId = null;

                if (!string.IsNullOrWhiteSpace(request.VoucherCode))
                {
                    // Lấy CategoryID của tất cả sách trong giỏ hàng để kiểm tra scope
                    var bookIds = request.CartItems
                        .Where(i => i.BookID.HasValue)
                        .Select(i => i.BookID!.Value)
                        .ToList();

                    var bookCategoryIds = await _context.Set<RealBook>()
                        .Where(b => bookIds.Contains(b.BookID))
                        .Select(b => b.CategoryID)
                        .Distinct()
                        .ToListAsync();

                    var blindBookIds = request.CartItems
                        .Where(i => i.BlindBookID.HasValue)
                        .Select(i => i.BlindBookID!.Value)
                        .ToList();

                    if (blindBookIds.Any())
                    {
                        var blindBookCategories = await _context.Set<BlindBook>()
                            .Include(b => b.RealBook)
                            .Where(b => blindBookIds.Contains(b.BlindBookID) && b.RealBook != null)
                            .Select(b => b.RealBook!.CategoryID)
                            .Distinct()
                            .ToListAsync();

                        bookCategoryIds = bookCategoryIds.Union(blindBookCategories).Distinct().ToList();
                    }

                    var voucherResult = await _voucherService.ValidateAndApplyVoucherAsync(
                        customerId,
                        request.VoucherCode,
                        subTotal,
                        bookCategoryIds);

                    if (!voucherResult.IsValid)
                        throw new InvalidOperationException($"Voucher không hợp lệ: {voucherResult.ErrorMessage}");

                    discountAmount = voucherResult.DiscountAmount;
                    if (voucherResult.VoucherID.HasValue)
                    {
                        appliedVoucherId = voucherResult.VoucherID.Value;
                        order.VoucherID = appliedVoucherId;
                    }
                    order.DiscountAmount = discountAmount;
                }

                order.TotalAmount = Math.Max(0, subTotal + (order.ShippingFee ?? 0) - discountAmount);

                await _context.Set<Order>().AddAsync(order);
                await _context.SaveChangesAsync();

                // Đánh dấu voucher đã được dùng sau khi tạo đơn thành công
                if (appliedVoucherId.HasValue)
                {
                    await _voucherService.MarkVoucherAsUsedAsync(customerId, appliedVoucherId.Value, order.OrderID);
                }

                await transaction.CommitAsync();

                return new OrderResponseDTO
                {
                    OrderID = order.OrderID,
                    TotalAmount = order.TotalAmount,
                    OrderStatus = order.OrderStatus,
                    OrderDate = order.OrderDate ?? DateTime.UtcNow,
                    PaymentUrl = order.PaymentMethod != PaymentMethod.COD ? $"https://vnpay.vn/mock-payment-gateway?orderId={order.OrderID}" : null
                };
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // --- QUẢN LÝ ĐƠN HÀNG PHÍA CUSTOMER ---
        public async Task<IEnumerable<OrderListItemDTO>> GetOrdersForCustomerAsync(long customerId, OrderStatus? status)
        {
            // Lọc đơn hàng theo chính CustomerID để đảm bảo bảo mật dữ liệu khách hàng
            var query = _context.Set<Order>()
                .Where(o => o.CustomerID == customerId);

            // Nếu truyền vào status thì lọc theo trạng thái (Tab UI: Chờ xác nhận, Đang giao, Đã giao...)
            if (status.HasValue)
            {
                query = query.Where(o => o.OrderStatus == status.Value);
            }

            // Sắp xếp đơn hàng mới nhất lên đầu
            var orders = await query
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            // Lấy thông tin tên hiển thị của Customer từ DbContext
            var user = await _context.Users.FindAsync(customerId);
            string customerName = user != null ? $"{user.LastName} {user.FirstName}".Trim() : "Khách hàng";

            // Ánh xạ sang DTO trả về cho Client hiển thị lên danh sách lịch sử mua hàng
            return orders.Select(o => new OrderListItemDTO
            {
                OrderID = o.OrderID,
                CustomerID = o.CustomerID,
                CustomerName = customerName,
                OrderDate = o.OrderDate ?? DateTime.UtcNow,
                OrderStatus = o.OrderStatus,
                PaymentMethod = o.PaymentMethod,
                PaymentStatus = o.PaymentStatus,
                TotalAmount = o.TotalAmount,
                ShipReceiverName = o.ShipReceiverName,
                ShipPhoneNumber = o.ShipPhoneNumber,
                Note = o.Note
            });
        }

        // --- QUẢN LÝ ĐƠN HÀNG (STORE MANAGER) ---

        public async Task<IEnumerable<OrderListItemDTO>> GetOrdersForStoreAsync(OrderStatus? status, string? searchTerm)
        {
            var query = _context.Set<Order>().AsQueryable();

            if (status.HasValue)
            {
                query = query.Where(o => o.OrderStatus == status.Value);
            }

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                string term = searchTerm.Trim().ToLower();
                query = query.Where(o => o.OrderID.ToString() == term || 
                                         o.ShipReceiverName.ToLower().Contains(term) || 
                                         o.ShipPhoneNumber.Contains(term));
            }

            var orders = await query
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            var customerIds = orders.Select(o => o.CustomerID).Distinct().ToList();
            var users = await _context.Users
                .Where(u => customerIds.Contains(u.UserID))
                .ToDictionaryAsync(u => u.UserID, u => $"{u.LastName} {u.FirstName}".Trim());

            return orders.Select(o => new OrderListItemDTO
            {
                OrderID = o.OrderID,
                CustomerID = o.CustomerID,
                CustomerName = users.TryGetValue(o.CustomerID, out var name) && !string.IsNullOrWhiteSpace(name) ? name : "Khách hàng ẩn danh",
                OrderDate = o.OrderDate ?? DateTime.UtcNow,
                OrderStatus = o.OrderStatus,
                PaymentMethod = o.PaymentMethod,
                PaymentStatus = o.PaymentStatus,
                TotalAmount = o.TotalAmount,
                ShipReceiverName = o.ShipReceiverName,
                ShipPhoneNumber = o.ShipPhoneNumber,
                Note = o.Note
            });
        }

        public async Task<OrderDetailDTO?> GetOrderDetailForStoreAsync(long orderId)
        {
            var order = await _context.Set<Order>()
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.RealBook)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.BlindBook)
                .FirstOrDefaultAsync(o => o.OrderID == orderId);

            if (order == null) return null;

            var user = await _context.Users.FindAsync(order.CustomerID);
            string customerName = user != null ? $"{user.LastName} {user.FirstName}".Trim() : "Khách hàng ẩn danh";

            var dto = new OrderDetailDTO
            {
                OrderID = order.OrderID,
                CustomerID = order.CustomerID,
                CustomerName = customerName,
                CustomerEmail = user?.Email ?? string.Empty,
                CustomerPhoneNumber = user?.PhoneNumber ?? string.Empty,
                OrderDate = order.OrderDate ?? DateTime.UtcNow,
                ShippedDate = order.ShippedDate,
                DeliveredDate = order.DeliveredDate,
                CompletedDate = order.CompletedDate,
                OrderStatus = order.OrderStatus,
                PaymentMethod = order.PaymentMethod,
                PaymentStatus = order.PaymentStatus,
                ShippingFee = order.ShippingFee ?? 0,
                DiscountAmount = order.DiscountAmount ?? 0,
                TotalAmount = order.TotalAmount,
                ShipReceiverName = order.ShipReceiverName,
                ShipPhoneNumber = order.ShipPhoneNumber,
                ShipDetailAddress = order.ShipDetailAddress,
                Note = order.Note,
                OrderItems = order.OrderDetails.Select(od => new OrderItemDTO
                {
                    BookID = od.BookID,
                    BlindBookID = od.BlindBookID,
                    Title = od.BlindBookID.HasValue && od.BlindBook != null
                        ? $"[Sách Mù] {od.BlindBook.Category}"
                        : od.RealBook?.Title ?? "Sách không xác định",
                    UnitPrice = od.UnitPrice,
                    Quantity = od.Quantity,
                    Discount = od.Discount ?? 0,
                    TotalItemAmount = od.UnitPrice * od.Quantity - (od.Discount ?? 0),
                    SampleFilePath = od.RealBook?.SampleFilePath,
                    ISBN = od.RealBook?.ISBN ?? string.Empty,
                    Publisher = od.RealBook?.Publisher ?? string.Empty
                }).ToList()
            };

            return dto;
        }

        public async Task<bool> ConfirmOrdersAsync(List<long> orderIds)
        {
            if (orderIds == null || !orderIds.Any()) return false;

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var orders = await _context.Set<Order>()
                    .Include(o => o.OrderDetails)
                    .Where(o => orderIds.Contains(o.OrderID) && o.OrderStatus == OrderStatus.Pending)
                    .ToListAsync();

                if (!orders.Any()) return false;

                foreach (var order in orders)
                {
                    order.OrderStatus = OrderStatus.AwaitingPickup;

                    foreach (var detail in order.OrderDetails)
                    {
                        var realBook = await _context.Set<RealBook>().FindAsync(detail.BookID);
                        if (realBook != null)
                        {
                            realBook.ReservedQuantity -= detail.Quantity;
                            if (realBook.ReservedQuantity < 0) realBook.ReservedQuantity = 0;
                        }
                    }
                }

                await _context.SaveChangesAsync();

                // Send notifications to customers
                foreach (var order in orders)
                {
                    try
                    {
                        await _notificationService.CreateAndSendNotificationAsync(
                            order.CustomerID,
                            "Đơn hàng được xác nhận",
                            $"Đơn hàng #{order.OrderID} của bạn đã được xác nhận và đang chờ lấy hàng.",
                            NotificationType.OrderStatus,
                            (int)order.OrderID
                        );
                    }
                    catch { /* Suppress notification errors */ }
                }

                await transaction.CommitAsync();
                return true;
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<bool> UpdateOrdersStatusAsync(List<long> orderIds, OrderStatus status)
        {
            if (orderIds == null || !orderIds.Any()) return false;

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var orders = await _context.Set<Order>()
                    .Include(o => o.OrderDetails)
                    .Where(o => orderIds.Contains(o.OrderID))
                    .ToListAsync();

                if (!orders.Any()) return false;

                foreach (var order in orders)
                {
                    var oldStatus = order.OrderStatus;
                    if (oldStatus == status) continue;

                    order.OrderStatus = status;

                    if (status == OrderStatus.Shipping)
                    {
                        order.ShippedDate = DateTime.UtcNow;
                    }
                    else if (status == OrderStatus.Delivering)
                    {
                        order.DeliveredDate = DateTime.UtcNow;
                    }
                    else if (status == OrderStatus.Completed)
                    {
                        order.CompletedDate = DateTime.UtcNow;
                        order.PaymentStatus = 1; // Đã thanh toán khi hoàn thành đơn
                    }
                    else if (status == OrderStatus.Cancelled)
                    {
                        // HOÀN TRẢ LẠI KHO HÀNG
                        foreach (var detail in order.OrderDetails)
                        {
                            var realBook = await _context.Set<RealBook>().FindAsync(detail.BookID);
                            if (realBook != null)
                            {
                                // Cộng trả lại UnitsInStock
                                realBook.UnitsInStock += detail.Quantity;

                                // Nếu hủy từ trạng thái Pending, ta cần giảm cả ReservedQuantity (vì lúc checkout đã tăng)
                                if (oldStatus == OrderStatus.Pending)
                                {
                                    realBook.ReservedQuantity -= detail.Quantity;
                                    if (realBook.ReservedQuantity < 0) realBook.ReservedQuantity = 0;
                                }
                            }

                            // Nếu là sách mù, cộng trả lại số lượng cho gói sách mù
                            if (detail.BlindBookID.HasValue)
                            {
                                var blindBook = await _context.Set<BlindBook>().FindAsync(detail.BlindBookID.Value);
                                if (blindBook != null)
                                {
                                    blindBook.StockQuantity += detail.Quantity;
                                }
                            }
                        }

                        // HOÀN TRẢ VOUCHER (nếu IsAutoRefundable = true)
                        try
                        {
                            await _voucherService.RefundVoucherIfApplicableAsync(order.OrderID);
                        }
                        catch { /* Suppress voucher refund errors - không để lỗi này block hủy đơn */ }
                    }
                }

                await _context.SaveChangesAsync();

                if (status == OrderStatus.Completed)
                {
                    var completedCustomerIds = orders.Select(o => o.CustomerID).Distinct();
                    foreach (var cId in completedCustomerIds)
                    {
                        try
                        {
                            await _gamificationService.CheckAndGrantShoppingBadgesAsync(cId);
                        }
                        catch (Exception ex)
                        {
                            // Suppress/log badge check errors
                        }
                    }
                }

                // Send notifications to customers
                foreach (var order in orders)
                {
                    try
                    {
                        string title = "Cập nhật trạng thái đơn hàng";
                        string content = $"Đơn hàng #{order.OrderID} đã thay đổi trạng thái.";
                        switch (status)
                        {
                            case OrderStatus.AwaitingPickup:
                                title = "Đơn hàng chờ lấy";
                                content = $"Đơn hàng #{order.OrderID} đang chờ đơn vị vận chuyển lấy hàng.";
                                break;
                            case OrderStatus.Shipping:
                                title = "Đơn hàng đang giao";
                                content = $"Đơn hàng #{order.OrderID} của bạn đã được gửi đi và đang vận chuyển.";
                                break;
                            case OrderStatus.Delivering:
                                title = "Đơn hàng đang giao đến bạn";
                                content = $"Đơn hàng #{order.OrderID} đang được giao đến địa chỉ của bạn.";
                                break;
                            case OrderStatus.Completed:
                                title = "Đơn hàng hoàn thành";
                                content = $"Đơn hàng #{order.OrderID} đã hoàn thành thành công. Cảm ơn bạn đã mua hàng!";
                                break;
                            case OrderStatus.Cancelled:
                                title = "Đơn hàng bị hủy";
                                content = $"Đơn hàng #{order.OrderID} của bạn đã bị hủy.";
                                break;
                        }

                        await _notificationService.CreateAndSendNotificationAsync(
                            order.CustomerID,
                            title,
                            content,
                            NotificationType.OrderStatus,
                            (int)order.OrderID
                        );
                    }
                    catch { /* Suppress notification errors */ }
                }

                await transaction.CommitAsync();
                return true;
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<byte[]> GenerateInvoicePdfAsync(long orderId)
        {
            var model = await GetOrderDetailForStoreAsync(orderId);
            if (model == null) throw new KeyNotFoundException($"Không tìm thấy đơn hàng ID {orderId}");

            var document = new InvoiceDocument(model);
            return document.GeneratePdf();
        }

        public async Task<byte[]> GenerateInvoicesPdfAsync(List<long> orderIds)
        {
            var models = new List<OrderDetailDTO>();
            foreach (var id in orderIds)
            {
                var model = await GetOrderDetailForStoreAsync(id);
                if (model != null) models.Add(model);
            }

            if (!models.Any()) throw new KeyNotFoundException("Không tìm thấy các đơn hàng được chọn.");

            var document = new InvoicesDocument(models);
            return document.GeneratePdf();
        }
    }

    // --- CÁC LỚP ĐỊNH NGHĨA FILE HÓA ĐƠN PDF DÙNG QUESTPDF ---

    internal class InvoiceDocument : IDocument
    {
        public OrderDetailDTO Model { get; }

        public InvoiceDocument(OrderDetailDTO model)
        {
            Model = model;
        }

        public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

        public void Compose(IDocumentContainer container)
        {
            container.Page(page =>
            {
                page.Margin(40);
                page.Size(PageSizes.A4);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                page.Header().Element(ComposeHeader);
                page.Content().Element(ComposeContent);
                page.Footer().Element(ComposeFooter);
            });
        }

        public void ComposeHeader(IContainer container)
        {
            container.Row(row =>
            {
                row.RelativeItem().Column(column =>
                {
                    column.Item().Text("HÓA ĐƠN BÁN HÀNG").FontSize(20).Bold().FontColor(Colors.Blue.Darken2);
                    column.Item().Text($"Mã hóa đơn: #{Model.OrderID}").FontSize(11).Bold();
                    column.Item().Text($"Ngày đặt: {Model.OrderDate:dd/MM/yyyy HH:mm}").FontSize(10);
                });

                row.ConstantItem(180).Column(column =>
                {
                    column.Item().Text("BookBlossom Shop").FontSize(15).Bold().FontColor(Colors.Green.Darken2);
                    column.Item().Text("123 Đường Sách, TP. Đà Nẵng").FontSize(9);
                    column.Item().Text("Hotline: 1900 1234").FontSize(9);
                });
            });
        }

        public void ComposeContent(IContainer container)
        {
            container.PaddingTop(15).Column(column =>
            {
                column.Spacing(12);

                // Khách hàng & giao nhận
                column.Item().Border(0.5f).BorderColor(Colors.Grey.Lighten1).Padding(8).Row(row =>
                {
                    row.RelativeItem().Column(c =>
                    {
                        c.Item().Text("Thông Tin Khách Hàng").Bold().FontSize(11).FontColor(Colors.Blue.Darken2);
                        c.Item().Text($"Tên: {Model.CustomerName}");
                        c.Item().Text($"SĐT: {Model.CustomerPhoneNumber}");
                        c.Item().Text($"Email: {Model.CustomerEmail}");
                    });

                    row.RelativeItem().Column(c =>
                    {
                        c.Item().Text("Thông Tin Giao Hàng").Bold().FontSize(11).FontColor(Colors.Blue.Darken2);
                        c.Item().Text($"Người nhận: {Model.ShipReceiverName}");
                        c.Item().Text($"SĐT nhận: {Model.ShipPhoneNumber}");
                        c.Item().Text($"Địa chỉ: {Model.ShipDetailAddress}");
                        if (!string.IsNullOrEmpty(Model.Note))
                        {
                            c.Item().Text($"Ghi chú: {Model.Note}").Italic().FontSize(9);
                        }
                    });
                });

                // Bảng chi tiết sản phẩm
                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(30);
                        columns.RelativeColumn();
                        columns.ConstantColumn(80);
                        columns.ConstantColumn(50);
                        columns.ConstantColumn(80);
                    });

                    table.Header(header =>
                    {
                        header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("#").Bold();
                        header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Sản phẩm").Bold();
                        header.Cell().Background(Colors.Grey.Lighten3).Padding(5).AlignRight().Text("Đơn giá").Bold();
                        header.Cell().Background(Colors.Grey.Lighten3).Padding(5).AlignCenter().Text("SL").Bold();
                        header.Cell().Background(Colors.Grey.Lighten3).Padding(5).AlignRight().Text("Thành tiền").Bold();
                    });

                    int idx = 1;
                    foreach (var item in Model.OrderItems)
                    {
                        table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(idx++.ToString());
                        table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(item.Title);
                        table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text($"{item.UnitPrice:N0}đ");
                        table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignCenter().Text(item.Quantity.ToString());
                        table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text($"{item.TotalItemAmount:N0}đ");
                    }
                });

                // Tính toán tiền
                column.Item().AlignRight().Column(c =>
                {
                    c.Spacing(3);
                    // decimal subTotal = Model.TotalAmount - Model.ShippingFee + Model.DiscountAmount;
                    decimal subTotal = Model.OrderItems.Sum(item => item.TotalItemAmount);
                    c.Item().Text($"Tạm tính: {subTotal:N0}đ");
                    c.Item().Text($"Phí vận chuyển: {Model.ShippingFee:N0}đ");
                    if (Model.DiscountAmount > 0)
                    {
                        c.Item().Text($"Khuyến mãi: -{Model.DiscountAmount:N0}đ");
                    }
                    c.Item().Text($"Tổng thanh toán: {Model.TotalAmount:N0}đ").FontSize(13).Bold().FontColor(Colors.Red.Medium);
                    c.Item().Text($"Thanh toán qua: {GetPaymentMethodName(Model.PaymentMethod)} | Trạng thái: {(Model.PaymentStatus == 1 ? "Đã thanh toán" : "Chưa thanh toán")}").FontSize(9);
                });
            });
        }

        public void ComposeFooter(IContainer container)
        {
            container.AlignCenter().Column(c =>
            {
                c.Item().Text("Cảm ơn quý khách đã mua hàng tại BookBlossom!").Italic().FontSize(9);
                c.Item().Text("Hóa đơn được in tự động từ hệ thống.").FontSize(8).FontColor(Colors.Grey.Medium);
            });
        }

        private string GetPaymentMethodName(PaymentMethod method)
        {
            return method switch
            {
                PaymentMethod.COD => "Thanh toán khi nhận hàng (COD)",
                PaymentMethod.VNPay => "VNPay Online",
                _ => method.ToString()
            };
        }
    }

    internal class InvoicesDocument : IDocument
    {
        public List<OrderDetailDTO> Models { get; }

        public InvoicesDocument(List<OrderDetailDTO> models)
        {
            Models = models;
        }

        public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

        public void Compose(IDocumentContainer container)
        {
            foreach (var model in Models)
            {
                container.Page(page =>
                {
                    page.Margin(40);
                    page.Size(PageSizes.A4);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                    var doc = new InvoiceDocument(model);
                    page.Header().Element(doc.ComposeHeader);
                    page.Content().Element(doc.ComposeContent);
                    page.Footer().Element(doc.ComposeFooter);
                });
            }
        }
    }
}