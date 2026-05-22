using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BookBlossom.Core.DTOs.CheckoutAndCreateOrder;
using BookBlossom.Core.Entities;
using BookBlossom.Core.Enums;
using BookBlossom.Core.Interfaces;
using BookBlossom.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BookBlossom.Infrastructure.Services
{
    public class OrderService : IOrderService
    {
        private readonly ApplicationDbContext _context;

        public OrderService(ApplicationDbContext context)
        {
            _context = context;
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
                // Hệ thống tự động LookUp thông tin địa chỉ từ AddressID đã chốt ở Frontend
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
                    
                    // Điền tự động dữ liệu có sẵn từ bảng địa chỉ vào hóa đơn lịch sử đơn hàng
                    ShipReceiverName = deliveryAddress.ReceiverName,
                    ShipPhoneNumber = deliveryAddress.PhoneNumber,
                    ShipDetailAddress = deliveryAddress.DetailAddress,
                    
                    Note = null, 
                    ShippingFee = 30000, // Phí vận chuyển mặc định
                    DiscountAmount = 0   // Chưa làm nghiệp vụ Voucher nên mặc định bằng 0
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

                        // Chỉ cần kiểm tra số lượng tồn kho của gói Sách Mù đã được cấp phát
                        if (blindBook.StockQuantity < item.Quantity)
                        {
                            throw new InvalidOperationException($"Số lượng gói Sách Mù hiện không đủ (Chỉ còn {blindBook.StockQuantity} gói).");
                        }

                        // Trừ số lượng kho của gói Sách Mù
                        blindBook.StockQuantity -= item.Quantity;

                        // Đồng thời trừ kho vật lý của sách thật (Nếu cần thiết để thống nhất kho kép)
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
                            BookID = blindBook.RealBookID, // Tự động lấy BookID từ bản ghi BlindBook, không phụ thuộc vào request của Frontend
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
                            BookID = item.BookID.Value, // .Value an toàn vì đã check HasValue ở trên
                            BlindBookID = null,
                            UnitPrice = unitPrice,
                            Quantity = item.Quantity,
                            Discount = 0
                        });
                    }
                }

                // 6. Tính toán tổng số tiền thanh toán cuối cùng (Tổng = Tiền sách + Ship)
                order.TotalAmount = subTotal + (order.ShippingFee ?? 0);

                // 7. Lưu dữ liệu vào Database
                await _context.Set<Order>().AddAsync(order);
                await _context.SaveChangesAsync();

                // 8. Hoàn tất chuỗi giao dịch thành công
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
    }
}