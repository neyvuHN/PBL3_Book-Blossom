using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BookBlossom.Core.DTOs;
using BookBlossom.Core.Entities;
using BookBlossom.Core.Enums;
using BookBlossom.Core.Interfaces.Services;
using BookBlossom.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BookBlossom.Infrastructure.Services
{
    public class VoucherService : IVoucherService
    {
        private readonly ApplicationDbContext _context;
        private static readonly DateTime SentinelDate = new DateTime(2099, 12, 31);

        public VoucherService(ApplicationDbContext context)
        {
            _context = context;
        }

        // ─── CRUD ───────────────────────────────────────────────────────────

        public async Task<IEnumerable<VoucherDTO>> GetAllVouchersAsync()
        {
            await AutoUpdateVoucherStatusesAsync();
            var vouchers = await _context.Vouchers
                .Include(v => v.VoucherCategories)
                .Include(v => v.VoucherBooks)
                .OrderByDescending(v => v.VoucherID)
                .ToListAsync();

            return vouchers.Select(MapToDTO);
        }

        public async Task<VoucherDTO?> GetVoucherByIdAsync(long voucherId)
        {
            await AutoUpdateVoucherStatusesAsync();
            var voucher = await _context.Vouchers
                .Include(v => v.VoucherCategories)
                .Include(v => v.VoucherBooks)
                .FirstOrDefaultAsync(v => v.VoucherID == voucherId);

            return voucher == null ? null : MapToDTO(voucher);
        }

        public async Task<VoucherDTO> CreateVoucherAsync(CreateVoucherDTO dto)
        {
            // Kiểm tra mã voucher không được trùng
            bool codeExists = await _context.Vouchers.AnyAsync(v => v.VoucherCode == dto.VoucherCode);
            if (codeExists)
                throw new InvalidOperationException($"Mã voucher '{dto.VoucherCode}' đã tồn tại.");

            if (dto.EndDate <= dto.StartDate)
                throw new InvalidOperationException("Thời gian kết thúc phải lớn hơn thời gian bắt đầu.");

            var voucher = new Voucher
            {
                VoucherName = dto.VoucherName,
                VoucherCode = dto.VoucherCode,
                DiscountType = dto.DiscountType,
                DiscountValue = dto.DiscountValue,
                MaxDiscountAmount = dto.MaxDiscountAmount,
                MinOrderValue = dto.MinOrderValue,
                TotalLimit = dto.TotalLimit,
                UsedCount = 0,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                StatusVoucher = dto.StatusVoucher,
                MinReputationRequired = dto.MinReputationRequired,
                MembershipRankRequired = dto.MembershipRankRequired,
                MinPlan = dto.MinPlan,
                IsForNewUser = dto.IsForNewUser,
                RequiredBadgeID = dto.RequiredBadgeID,
                IsStackable = dto.IsStackable,
                IsAutoRefundable = dto.IsAutoRefundable,
                MaxUsagePerUser = dto.MaxUsagePerUser
            };

            _context.Vouchers.Add(voucher);
            await _context.SaveChangesAsync();

            // Thêm các category áp dụng (nếu có)
            foreach (var catId in dto.ApplicableCategoryIDs)
            {
                _context.VoucherCategories.Add(new VoucherCategory
                {
                    VoucherID = voucher.VoucherID,
                    CategoryID = catId
                });
            }

            // Thêm các sách áp dụng (nếu có)
            foreach (var bookId in dto.ApplicableBookIDs)
            {
                _context.VoucherBooks.Add(new VoucherBook
                {
                    VoucherID = voucher.VoucherID,
                    BookID = bookId
                });
            }
            await _context.SaveChangesAsync();

            return MapToDTO(await _context.Vouchers
                .Include(v => v.VoucherCategories)
                .Include(v => v.VoucherBooks)
                .FirstAsync(v => v.VoucherID == voucher.VoucherID));
        }

        public async Task<VoucherDTO?> UpdateVoucherAsync(long voucherId, UpdateVoucherDTO dto)
        {
            var voucher = await _context.Vouchers
                .Include(v => v.VoucherCategories)
                .Include(v => v.VoucherBooks)
                .FirstOrDefaultAsync(v => v.VoucherID == voucherId);

            if (voucher == null) return null;



            var checkStart = dto.StartDate ?? voucher.StartDate;
            var checkEnd = dto.EndDate ?? voucher.EndDate;
            if (checkEnd <= checkStart)
                throw new InvalidOperationException("Thời gian kết thúc phải lớn hơn thời gian bắt đầu.");

            if (dto.VoucherName != null) voucher.VoucherName = dto.VoucherName;
            if (dto.DiscountType.HasValue) voucher.DiscountType = dto.DiscountType.Value;
            if (dto.DiscountValue.HasValue) voucher.DiscountValue = dto.DiscountValue.Value;
            if (dto.MaxDiscountAmount.HasValue) voucher.MaxDiscountAmount = dto.MaxDiscountAmount.Value;
            if (dto.MinOrderValue.HasValue) voucher.MinOrderValue = dto.MinOrderValue.Value;
            if (dto.TotalLimit.HasValue) voucher.TotalLimit = dto.TotalLimit.Value;
            if (dto.StartDate.HasValue) voucher.StartDate = dto.StartDate.Value;
            if (dto.EndDate.HasValue) voucher.EndDate = dto.EndDate.Value;
            if (dto.StatusVoucher.HasValue) voucher.StatusVoucher = dto.StatusVoucher.Value;
            if (dto.MinReputationRequired.HasValue) voucher.MinReputationRequired = dto.MinReputationRequired.Value;
            if (dto.MembershipRankRequired.HasValue) voucher.MembershipRankRequired = dto.MembershipRankRequired.Value;
            if (dto.MinPlan.HasValue) voucher.MinPlan = dto.MinPlan.Value;
            if (dto.IsForNewUser.HasValue) voucher.IsForNewUser = dto.IsForNewUser.Value;
            if (dto.RequiredBadgeID.HasValue) voucher.RequiredBadgeID = dto.RequiredBadgeID.Value;
            if (dto.IsStackable.HasValue) voucher.IsStackable = dto.IsStackable.Value;
            if (dto.IsAutoRefundable.HasValue) voucher.IsAutoRefundable = dto.IsAutoRefundable.Value;
            if (dto.MaxUsagePerUser.HasValue) voucher.MaxUsagePerUser = dto.MaxUsagePerUser.Value;

            // Cập nhật danh sách category
            if (dto.ApplicableCategoryIDs != null)
            {
                _context.VoucherCategories.RemoveRange(voucher.VoucherCategories);
                foreach (var catId in dto.ApplicableCategoryIDs)
                {
                    _context.VoucherCategories.Add(new VoucherCategory
                    {
                        VoucherID = voucher.VoucherID,
                        CategoryID = catId
                    });
                }
            }

            // Cập nhật danh sách sách áp dụng
            if (dto.ApplicableBookIDs != null)
            {
                _context.VoucherBooks.RemoveRange(voucher.VoucherBooks);
                foreach (var bookId in dto.ApplicableBookIDs)
                {
                    _context.VoucherBooks.Add(new VoucherBook
                    {
                        VoucherID = voucher.VoucherID,
                        BookID = bookId
                    });
                }
            }

            await _context.SaveChangesAsync();
            return MapToDTO(voucher);
        }

        public async Task<bool> DeleteVoucherAsync(long voucherId)
        {
            var voucher = await _context.Vouchers.FindAsync(voucherId);
            if (voucher == null) return false;

            _context.Vouchers.Remove(voucher);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<VoucherUsageStatsDTO?> GetVoucherStatsAsync(long voucherId)
        {
            var voucher = await _context.Vouchers.FindAsync(voucherId);
            if (voucher == null) return null;

            // Tính toán chính xác dựa trên dữ liệu thực tế từ các đơn hàng đã áp dụng
            decimal totalDiscountGranted = await _context.OrderVouchers
                .Where(ov => ov.VoucherID == voucherId)
                .SumAsync(ov => (decimal?)ov.DiscountAmount) ?? 0;    

            // Tổng doanh thu từ đơn có voucher
            var orderWithVoucher = await _context.OrderVouchers
                .Where(ov => ov.VoucherID == voucherId)
                .Select(ov => ov.Order)
                .SumAsync(o => (decimal?)o.TotalAmount) ?? 0;

            return new VoucherUsageStatsDTO
            {
                VoucherID = voucher.VoucherID,
                VoucherCode = voucher.VoucherCode,
                TotalLimit = voucher.TotalLimit,
                UsedCount = voucher.UsedCount,
                UsageRate = voucher.TotalLimit > 0 ? (double)voucher.UsedCount / voucher.TotalLimit * 100 : 0,
                TotalDiscountGranted = totalDiscountGranted,
                TotalRevenueGenerated = orderWithVoucher,
                ROI = totalDiscountGranted > 0 ? Math.Round(orderWithVoucher / totalDiscountGranted, 2) : 0
            };
        }

        public async Task<IEnumerable<VoucherUsageStatsDTO>> GetAllVoucherStatsAsync()
        {
            var stats = await _context.OrderVouchers
                .GroupBy(ov => ov.VoucherID)
                .Select(g => new {
                    VoucherID = g.Key,
                    TotalDiscount = g.Sum(x => (decimal?)x.DiscountAmount) ?? 0,
                    TotalRevenue = g.Select(x => x.Order).Sum(o => (decimal?)o.TotalAmount) ?? 0
                })
                .ToDictionaryAsync(x => x.VoucherID, x => x);

            var vouchers = await _context.Vouchers.ToListAsync();
            var result = new List<VoucherUsageStatsDTO>();
            foreach(var v in vouchers) {
                var s = stats.ContainsKey(v.VoucherID) ? stats[v.VoucherID] : null;
                var discount = s?.TotalDiscount ?? 0;
                var revenue = s?.TotalRevenue ?? 0;
                result.Add(new VoucherUsageStatsDTO {
                    VoucherID = v.VoucherID,
                    VoucherCode = v.VoucherCode,
                    TotalLimit = v.TotalLimit,
                    UsedCount = v.UsedCount,
                    UsageRate = v.TotalLimit > 0 ? (double)v.UsedCount / v.TotalLimit * 100 : 0,
                    TotalDiscountGranted = discount,
                    TotalRevenueGenerated = revenue,
                    ROI = discount > 0 ? Math.Round(revenue / discount, 2) : 0
                });
            }
            return result;
        }

        // ─── VÍ VOUCHER (CUSTOMER) ───────────────────────────────────────────

        public async Task<IEnumerable<CustomerVoucherDTO>> GetMyVouchersAsync(long customerId)
        {
            await AutoUpdateVoucherStatusesAsync();
            var customerVouchers = await _context.CustomerVouchers
                .Include(cv => cv.Voucher)
                    .ThenInclude(v => v.VoucherCategories)
                .Include(cv => cv.Voucher)
                    .ThenInclude(v => v.VoucherBooks)
                .Where(cv => cv.CustomerID == customerId 
                          && cv.Voucher.StatusVoucher != VoucherStatus.Paused
                          && cv.Voucher.StatusVoucher != VoucherStatus.Draft
                          && cv.Voucher.StatusVoucher != VoucherStatus.Ended)
                .OrderBy(cv => cv.IsUsed)
                .ThenBy(cv => cv.Voucher.EndDate)
                .ToListAsync();

            return customerVouchers.Select(cv => new CustomerVoucherDTO
            {
                VoucherID = cv.VoucherID,
                VoucherName = cv.Voucher.VoucherName,
                VoucherCode = cv.Voucher.VoucherCode,
                DiscountType = cv.Voucher.DiscountType,
                DiscountValue = cv.Voucher.DiscountValue,
                MaxDiscountAmount = cv.Voucher.MaxDiscountAmount,
                MinOrderValue = cv.Voucher.MinOrderValue,
                EndDate = cv.Voucher.EndDate,
                IsUsed = cv.IsUsed,
                OrderID = cv.OrderID,
                IsStackable = cv.Voucher.IsStackable,
                ApplicableCategoryIDs = cv.Voucher.VoucherCategories.Select(c => c.CategoryID).ToList(),
                ApplicableBookIDs = cv.Voucher.VoucherBooks.Select(b => b.BookID).ToList()
            });
        }

        public async Task<bool> ClaimVoucherAsync(long customerId, string voucherCode)
        {
            await AutoUpdateVoucherStatusesAsync();
            var voucher = await _context.Vouchers
                .FirstOrDefaultAsync(v => v.VoucherCode == voucherCode);

            if (voucher == null)
                throw new KeyNotFoundException($"Không tìm thấy voucher với mã '{voucherCode}'.");

            if (voucher.StatusVoucher != VoucherStatus.Active)
                throw new InvalidOperationException("Voucher này chưa kích hoạt hoặc đã kết thúc.");

            if (DateTime.UtcNow < voucher.StartDate || DateTime.UtcNow > voucher.EndDate)
                throw new InvalidOperationException("Voucher này không trong thời gian hiệu lực.");

            if (voucher.UsedCount >= voucher.TotalLimit)
                throw new InvalidOperationException("Voucher đã đạt giới hạn số lượng phát.");

            // 1. Kiểm tra xem trong ví ĐANG CÓ voucher này mà chưa dùng hay không
            bool hasUnused = await _context.CustomerVouchers
                .AnyAsync(cv => cv.CustomerID == customerId && cv.VoucherID == voucher.VoucherID && !cv.IsUsed);

            if (hasUnused)
                throw new InvalidOperationException("Bạn đã sở hữu voucher này trong ví và chưa sử dụng.");

            // 2. Kiểm tra TỔNG số lần đã thu thập/sử dụng so với giới hạn MaxUsagePerUser của Voucher
            int totalClaimedCount = await _context.CustomerVouchers
                .CountAsync(cv => cv.CustomerID == customerId && cv.VoucherID == voucher.VoucherID);

            if (totalClaimedCount >= voucher.MaxUsagePerUser)
                throw new InvalidOperationException($"Bạn đã đạt giới hạn thu thập voucher này (Tối đa {voucher.MaxUsagePerUser} lần).");

            _context.CustomerVouchers.Add(new CustomerVoucher
            {
                CustomerID = customerId,
                VoucherID = voucher.VoucherID,
                IsUsed = false,
                UsedAt = SentinelDate
            });

            await _context.SaveChangesAsync();
            return true;
        }

        // ─── CHECKOUT INTEGRATION ────────────────────────────────────────────

        public async Task<VoucherValidationResultDTO> ValidateAndApplyVoucherAsync(
            long customerId,
            List<string> voucherCodes,
            ICollection<OrderDetail> orderDetails)
        {
            await AutoUpdateVoucherStatusesAsync();
            var invalid = new VoucherValidationResultDTO { IsValid = false };

            if (voucherCodes == null || !voucherCodes.Any())
            {
                return new VoucherValidationResultDTO { IsValid = true, TotalDiscountAmount = 0 };
            }

            var result = new VoucherValidationResultDTO { IsValid = true };
            var vouchers = new List<Voucher>();

            // 1. Lấy thông tin voucher
            foreach (var code in voucherCodes)
            {
                var v = await _context.Vouchers
                    .Include(x => x.VoucherCategories)
                    .Include(x => x.VoucherBooks)
                    .FirstOrDefaultAsync(x => x.VoucherCode == code);

                if (v == null)
                {
                    invalid.ErrorMessage = $"Voucher {code} does not exist.";
                    return invalid;
                }
                vouchers.Add(v);
            }

            // 2. Kiểm tra IsStackable (Quy tắc độc quyền)
            if (vouchers.Count > 1 && vouchers.Any(v => !v.IsStackable))
            {
                invalid.ErrorMessage = "Có voucher không cho phép cộng dồn. Vui lòng chỉ chọn 1 voucher duy nhất nếu có voucher không hỗ trợ áp dụng cùng lúc.";
                return invalid;
            }

            // 3. Phân loại Voucher: Toàn Sàn và Sản Phẩm
            var productVouchers = vouchers.Where(v => v.VoucherCategories.Any() || v.VoucherBooks.Any())
                // Ưu tiên Percentage trước, sau đó là Fixed cao hơn
                .OrderByDescending(v => v.DiscountType == VoucherDiscountType.Percentage ? 1 : 0)
                .ThenByDescending(v => v.DiscountValue)
                .ToList();

            var platformVouchers = vouchers.Where(v => !v.VoucherCategories.Any() && !v.VoucherBooks.Any()).ToList();

            // 4. Chuẩn bị dữ liệu CartLines
            var cartLines = new List<dynamic>();
            foreach (var od in orderDetails)
            {
                long categoryId = 0;
                if (od.BlindBookID.HasValue)
                {
                    var bb = await _context.BlindBooks.Include(b => b.RealBook).FirstOrDefaultAsync(b => b.BlindBookID == od.BlindBookID.Value);
                    if (bb != null && bb.RealBook != null) categoryId = bb.RealBook.CategoryID;
                }
                else
                {
                    var rb = await _context.RealBooks.FindAsync(od.BookID);
                    if (rb != null) categoryId = rb.CategoryID;
                }

                cartLines.Add(new
                {
                    OrderDetail = od,
                    BookID = od.BookID,
                    CategoryID = categoryId,
                    IsBlindDate = od.BlindBookID.HasValue
                });
            }

            // Theo dõi CurrentValue của từng OrderDetail
            var lineValues = cartLines.ToDictionary(cl => (OrderDetail)cl.OrderDetail, cl => ((OrderDetail)cl.OrderDetail).UnitPrice * ((OrderDetail)cl.OrderDetail).Quantity);

            // Kiểm tra các quy tắc chung cho từng voucher
            var now = DateTime.UtcNow;
            foreach (var v in vouchers)
            {
                if (v.StatusVoucher != VoucherStatus.Active) return new VoucherValidationResultDTO { IsValid = false, ErrorMessage = $"Voucher {v.VoucherCode} not active." };
                if (now < v.StartDate || now > v.EndDate) return new VoucherValidationResultDTO { IsValid = false, ErrorMessage = $"Voucher {v.VoucherCode} không trong thời gian hiệu lực." };
                if (v.UsedCount >= v.TotalLimit) return new VoucherValidationResultDTO { IsValid = false, ErrorMessage = $"Voucher {v.VoucherCode} đã hết lượt sử dụng." };

                int timesUsed = await _context.CustomerVouchers.CountAsync(cv => cv.CustomerID == customerId && cv.VoucherID == v.VoucherID && cv.IsUsed);
                if (timesUsed >= v.MaxUsagePerUser) return new VoucherValidationResultDTO { IsValid = false, ErrorMessage = $"Bạn đã dùng voucher {v.VoucherCode} tối đa số lần cho phép." };

                if (v.MinReputationRequired > 0)
                {
                    var rep = await _context.CustomerReputations.FirstOrDefaultAsync(r => r.CustomerID == customerId);
                    if ((rep?.ReputationPoint ?? 100) < v.MinReputationRequired) return new VoucherValidationResultDTO { IsValid = false, ErrorMessage = $"Điểm uy tín không đủ để dùng voucher {v.VoucherCode}." };
                }

                if (v.MembershipRankRequired > 0)
                {
                    var customer = await _context.CustomerDetails.Include(c => c.MembershipRank).FirstOrDefaultAsync(c => c.CustomerID == customerId);
                    if ((customer?.MembershipRank?.RankType ?? 0) < v.MembershipRankRequired) return new VoucherValidationResultDTO { IsValid = false, ErrorMessage = $"Hạng thành viên không đủ để dùng voucher {v.VoucherCode}." };
                }

                if (v.MinPlan > SubscriptionType.Free)
                {
                    var cs = await _context.CustomerServices.Include(x => x.ServicePackage).FirstOrDefaultAsync(x => x.CustomerID == customerId);
                    SubscriptionType currentPlan = SubscriptionType.Free;
                    if (cs != null && (cs.EndDate == null || cs.EndDate >= now))
                    {
                        var pkg = cs.ServicePackage?.PackageName?.ToLower() ?? "";
                        if (pkg.Contains("pro")) currentPlan = SubscriptionType.Pro;
                        else if (pkg.Contains("basic")) currentPlan = SubscriptionType.Basic;
                    }
                    if (currentPlan < v.MinPlan) return new VoucherValidationResultDTO { IsValid = false, ErrorMessage = $"Gói dịch vụ không đủ để dùng voucher {v.VoucherCode}." };
                }

                if (v.RequiredBadgeID.HasValue)
                {
                    bool hasBadge = await _context.BadgeCustomers.AnyAsync(bc => bc.CustomerID == customerId && bc.BadgeID == v.RequiredBadgeID.Value);
                    if (!hasBadge) return new VoucherValidationResultDTO { IsValid = false, ErrorMessage = $"Bạn chưa có huy hiệu yêu cầu cho voucher {v.VoucherCode}." };
                }

                if (v.IsForNewUser)
                {
                    int orderCount = await _context.Orders.CountAsync(o => o.CustomerID == customerId);
                    if (orderCount > 0) return new VoucherValidationResultDTO { IsValid = false, ErrorMessage = $"Voucher {v.VoucherCode} chỉ dành cho khách hàng mới." };
                }
            }

            // 5. Áp dụng Voucher Sản Phẩm
            foreach (var v in productVouchers)
            {
                var reqCatIds = v.VoucherCategories.Select(vc => vc.CategoryID).ToList();
                var reqBookIds = v.VoucherBooks.Select(vb => vb.BookID).ToList();

                var eligibleLines = cartLines.Where(cl => 
                {
                    bool isBlindDate = (bool)cl.IsBlindDate;
                    long catId = (long)cl.CategoryID;
                    long bookId = (long)cl.BookID;

                    if (isBlindDate)
                    {
                        return reqCatIds.Any() && reqCatIds.Contains(catId);
                    }
                    else
                    {
                        return (reqCatIds.Any() && reqCatIds.Contains(catId)) ||
                               (reqBookIds.Any() && reqBookIds.Contains(bookId));
                    }
                }).ToList();

                decimal eligibleTotal = eligibleLines.Sum(cl => lineValues[(OrderDetail)cl.OrderDetail]);

                if (eligibleTotal < v.MinOrderValue)
                {
                    return new VoucherValidationResultDTO { IsValid = false, ErrorMessage = $"Chưa đủ giá trị tối thiểu sản phẩm ({v.MinOrderValue:N0}đ) cho voucher {v.VoucherCode}." };
                }

                if (!eligibleLines.Any()) continue;

                var targetLine = eligibleLines.OrderByDescending(cl => lineValues[(OrderDetail)cl.OrderDetail]).First();
                decimal currentValue = lineValues[(OrderDetail)targetLine.OrderDetail];

                decimal discount = 0;
                if (v.DiscountType == VoucherDiscountType.Fixed) discount = v.DiscountValue;
                else
                {
                    discount = currentValue * v.DiscountValue / 100;
                    if (v.MaxDiscountAmount > 0 && discount > v.MaxDiscountAmount) discount = v.MaxDiscountAmount;
                }

                discount = Math.Min(discount, currentValue);
                lineValues[(OrderDetail)targetLine.OrderDetail] -= discount;
                
                var actualOrderDetail = (OrderDetail)targetLine.OrderDetail;
                actualOrderDetail.Discount += discount;
                
                // Track applied product voucher
                string newBreakdown = $"{v.VoucherCode}:{discount}";
                if (string.IsNullOrEmpty(actualOrderDetail.VoucherBreakdown)) {
                    actualOrderDetail.VoucherBreakdown = newBreakdown;
                } else {
                    actualOrderDetail.VoucherBreakdown += "|" + newBreakdown;
                }

                result.AppliedVouchers.Add(new AppliedVoucherDTO { VoucherID = v.VoucherID, VoucherCode = v.VoucherCode, DiscountAmount = discount });
                result.TotalDiscountAmount += discount;
            }

            // 6. Áp dụng Voucher Toàn Sàn
            decimal currentSubTotal = lineValues.Values.Sum();
            foreach (var v in platformVouchers)
            {
                if (currentSubTotal < v.MinOrderValue)
                {
                    return new VoucherValidationResultDTO { IsValid = false, ErrorMessage = $"Giá trị giỏ hàng còn lại ({currentSubTotal:N0}đ) chưa đủ điều kiện {v.MinOrderValue:N0}đ cho voucher {v.VoucherCode}." };
                }

                decimal discount = 0;
                if (v.DiscountType == VoucherDiscountType.Fixed) discount = v.DiscountValue;
                else
                {
                    discount = currentSubTotal * v.DiscountValue / 100;
                    if (v.MaxDiscountAmount > 0 && discount > v.MaxDiscountAmount) discount = v.MaxDiscountAmount;
                }

                discount = Math.Min(discount, currentSubTotal);
                
                // Phân bổ giảm giá Toàn Sàn vào các OrderDetail theo tỷ lệ
                if (discount > 0 && currentSubTotal > 0)
                {
                    decimal remainingDiscount = discount;
                    var sortedLines = cartLines.OrderByDescending(cl => lineValues[(OrderDetail)cl.OrderDetail]).ToList();
                    
                    for (int i = 0; i < sortedLines.Count; i++)
                    {
                        var cl = sortedLines[i];
                        decimal val = lineValues[(OrderDetail)cl.OrderDetail];
                        if (val <= 0) continue;

                        decimal portion = (i == sortedLines.Count - 1) ? remainingDiscount : Math.Round(discount * (val / currentSubTotal), 2);
                        portion = Math.Min(portion, val);

                        lineValues[(OrderDetail)cl.OrderDetail] -= portion;
                        ((OrderDetail)cl.OrderDetail).Discount += portion;
                        remainingDiscount -= portion;
                    }
                }
                
                currentSubTotal -= discount;

                result.AppliedVouchers.Add(new AppliedVoucherDTO { VoucherID = v.VoucherID, VoucherCode = v.VoucherCode, DiscountAmount = discount });
                result.TotalDiscountAmount += discount;
            }

            return result;
        }

        public async Task MarkVoucherAsUsedAsync(long customerId, long voucherId, long orderId)
        {
            var customerVoucher = await _context.CustomerVouchers
                .FirstOrDefaultAsync(cv => cv.CustomerID == customerId
                                        && cv.VoucherID == voucherId
                                        && !cv.IsUsed);

            if (customerVoucher == null)
            {
                customerVoucher = new CustomerVoucher
                {
                    CustomerID = customerId,
                    VoucherID = voucherId,
                    IsUsed = false,
                    UsedAt = SentinelDate
                };
                _context.CustomerVouchers.Add(customerVoucher);
            }

            customerVoucher.IsUsed = true;
            customerVoucher.UsedAt = DateTime.UtcNow;
            customerVoucher.OrderID = orderId;

            // Tăng UsedCount trên Voucher
            var voucher = await _context.Vouchers.FindAsync(voucherId);
            if (voucher != null) voucher.UsedCount++;

            await _context.SaveChangesAsync();
        }

        public async Task RefundVoucherIfApplicableAsync(long orderId)
        {
            var orderVouchers = await _context.OrderVouchers
                .Include(ov => ov.Voucher)
                .Where(ov => ov.OrderID == orderId)
                .ToListAsync();

            if (!orderVouchers.Any()) return;

            foreach (var ov in orderVouchers)
            {
                var voucher = ov.Voucher;
                if (voucher == null || !voucher.IsAutoRefundable) continue;

                var customerVoucher = await _context.CustomerVouchers
                    .FirstOrDefaultAsync(cv => cv.OrderID == orderId
                                            && cv.VoucherID == voucher.VoucherID);

                if (customerVoucher == null) continue;

                // Hoàn trả: reset về chưa dùng
                customerVoucher.IsUsed = false;
                customerVoucher.UsedAt = SentinelDate;
                customerVoucher.OrderID = null;

                // Giảm UsedCount
                if (voucher.UsedCount > 0) voucher.UsedCount--;
            }

            await _context.SaveChangesAsync();
        }

        private async Task AutoUpdateVoucherStatusesAsync()
        {
            var now = DateTime.UtcNow;

            var vouchersToUpdate = await _context.Vouchers
                .Where(v => 
                    (v.StatusVoucher == VoucherStatus.Scheduled && v.StartDate <= now && v.EndDate > now) ||
                    (v.StatusVoucher != VoucherStatus.Ended && v.EndDate <= now) ||
                    (v.StatusVoucher == VoucherStatus.Active && v.StartDate > now))
                .ToListAsync();

            if (vouchersToUpdate.Any())
            {
                foreach (var v in vouchersToUpdate)
                {
                    if (v.StatusVoucher == VoucherStatus.Scheduled && v.StartDate <= now && v.EndDate > now)
                        v.StatusVoucher = VoucherStatus.Active;
                    else if (v.StatusVoucher != VoucherStatus.Ended && v.EndDate <= now)
                        v.StatusVoucher = VoucherStatus.Ended;
                    else if (v.StatusVoucher == VoucherStatus.Active && v.StartDate > now)
                        v.StatusVoucher = VoucherStatus.Scheduled;
                }
                await _context.SaveChangesAsync();
            }
        }

        private static VoucherDTO MapToDTO(Voucher v) => new VoucherDTO
        {
            VoucherID = v.VoucherID,
            VoucherName = v.VoucherName,
            VoucherCode = v.VoucherCode,
            DiscountType = v.DiscountType,
            DiscountValue = v.DiscountValue,
            MaxDiscountAmount = v.MaxDiscountAmount,
            MinOrderValue = v.MinOrderValue,
            TotalLimit = v.TotalLimit,
            UsedCount = v.UsedCount,
            StartDate = v.StartDate,
            EndDate = v.EndDate,
            StatusVoucher = v.StatusVoucher,
            MinReputationRequired = v.MinReputationRequired,
            MembershipRankRequired = v.MembershipRankRequired,
            MinPlan = v.MinPlan,
            IsForNewUser = v.IsForNewUser,
            RequiredBadgeID = v.RequiredBadgeID,
            IsStackable = v.IsStackable,
            IsAutoRefundable = v.IsAutoRefundable,
            MaxUsagePerUser = v.MaxUsagePerUser,
            ApplicableCategoryIDs = v.VoucherCategories?.Select(vc => vc.CategoryID).ToList() ?? new(),
            ApplicableBookIDs = v.VoucherBooks?.Select(vb => vb.BookID).ToList() ?? new()
        };
    }
}
