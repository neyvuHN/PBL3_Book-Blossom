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
                .OrderByDescending(v => v.VoucherID)
                .ToListAsync();

            return vouchers.Select(MapToDTO);
        }

        public async Task<VoucherDTO?> GetVoucherByIdAsync(long voucherId)
        {
            await AutoUpdateVoucherStatusesAsync();
            var voucher = await _context.Vouchers
                .Include(v => v.VoucherCategories)
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
            await _context.SaveChangesAsync();

            return MapToDTO(await _context.Vouchers
                .Include(v => v.VoucherCategories)
                .FirstAsync(v => v.VoucherID == voucher.VoucherID));
        }

        public async Task<VoucherDTO?> UpdateVoucherAsync(long voucherId, UpdateVoucherDTO dto)
        {
            var voucher = await _context.Vouchers
                .Include(v => v.VoucherCategories)
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
            decimal totalDiscountGranted = await _context.Orders
                .Where(o => o.VoucherID == voucherId)
                .SumAsync(o => (decimal?)o.DiscountAmount) ?? 0;    

            // Tổng doanh thu từ đơn có voucher
            var orderWithVoucher = await _context.Orders
                .Where(o => o.VoucherID == voucherId)
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

        // ─── VÍ VOUCHER (CUSTOMER) ───────────────────────────────────────────

        public async Task<IEnumerable<CustomerVoucherDTO>> GetMyVouchersAsync(long customerId)
        {
            await AutoUpdateVoucherStatusesAsync();
            var customerVouchers = await _context.CustomerVouchers
                .Include(cv => cv.Voucher)
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
                OrderID = cv.OrderID
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
            string voucherCode,
            decimal orderSubTotal,
            List<long> bookCategoryIds)
        {
            await AutoUpdateVoucherStatusesAsync();
            var invalid = new VoucherValidationResultDTO { IsValid = false };

            // 1. Tìm voucher theo mã
            var voucher = await _context.Vouchers
                .Include(v => v.VoucherCategories)
                .FirstOrDefaultAsync(v => v.VoucherCode == voucherCode);

            if (voucher == null)
            {
                invalid.ErrorMessage = "Voucher does not exist.";
                return invalid;
            }

            // 2. Kiểm tra trạng thái và thời hạn
            if (voucher.StatusVoucher != VoucherStatus.Active)
            {
                invalid.ErrorMessage = "Voucher not active or ended.";
                return invalid;
            }

            var now = DateTime.UtcNow;
            if (now < voucher.StartDate || now > voucher.EndDate)
            {
                invalid.ErrorMessage = "Voucher không trong thời gian hiệu lực.";
                return invalid;
            }

            // 3. Kiểm tra giới hạn tổng
            if (voucher.UsedCount >= voucher.TotalLimit)
            {
                invalid.ErrorMessage = "Voucher đã đạt giới hạn số lượng sử dụng.";
                return invalid;
            }

            // 5. Kiểm tra số lần đã dùng của customer
            int timesUsed = await _context.CustomerVouchers
                .CountAsync(cv => cv.CustomerID == customerId
                               && cv.VoucherID == voucher.VoucherID
                               && cv.IsUsed);

            if (timesUsed >= voucher.MaxUsagePerUser)
            {
                invalid.ErrorMessage = $"Bạn đã dùng voucher này tối đa {voucher.MaxUsagePerUser} lần.";
                return invalid;
            }

            // 6. Kiểm tra giá trị đơn hàng tối thiểu
            if (orderSubTotal < voucher.MinOrderValue)
            {
                invalid.ErrorMessage = $"Giá trị đơn hàng tối thiểu để dùng voucher này là {voucher.MinOrderValue:N0} VNĐ.";
                return invalid;
            }

            // 7. Kiểm tra điểm uy tín
            if (voucher.MinReputationRequired > 0)
            {
                var rep = await _context.CustomerReputations
                    .FirstOrDefaultAsync(r => r.CustomerID == customerId);
                int repPoint = rep?.ReputationPoint ?? 100;
                if (repPoint < voucher.MinReputationRequired)
                {
                    invalid.ErrorMessage = $"Điểm uy tín của bạn ({repPoint}) không đủ để dùng voucher này (yêu cầu {voucher.MinReputationRequired}).";
                    return invalid;
                }
            }

            // 8. Kiểm tra hạng thành viên
            if (voucher.MembershipRankRequired > 0)
            {
                var customer = await _context.CustomerDetails
                    .Include(c => c.MembershipRank)
                    .FirstOrDefaultAsync(c => c.CustomerID == customerId);

                byte customerRankType = customer?.MembershipRank?.RankType ?? 0;
                if (customerRankType < voucher.MembershipRankRequired)
                {
                    invalid.ErrorMessage = $"Hạng thành viên của bạn không đủ điều kiện để dùng voucher này.";
                    return invalid;
                }
            }

            // 8.5. Kiểm tra gói dịch vụ (MinPlan)
            if (voucher.MinPlan > SubscriptionType.Free)
            {
                var customerService = await _context.CustomerServices
                    .Include(cs => cs.ServicePackage)
                    .FirstOrDefaultAsync(cs => cs.CustomerID == customerId);

                SubscriptionType currentPlan = SubscriptionType.Free;
                if (customerService != null && (customerService.EndDate == null || customerService.EndDate >= DateTime.UtcNow))
                {
                    var pkgName = customerService.ServicePackage?.PackageName?.ToLower() ?? "";
                    if (pkgName.Contains("pro"))
                    {
                        currentPlan = SubscriptionType.Pro;
                    }
                    else if (pkgName.Contains("basic"))
                    {
                        currentPlan = SubscriptionType.Basic;
                    }
                }

                if (currentPlan < voucher.MinPlan)
                {
                    invalid.ErrorMessage = $"Gói dịch vụ của bạn không đủ điều kiện để dùng voucher này (yêu cầu tối thiểu {voucher.MinPlan}).";
                    return invalid;
                }
            }

            // 9. Kiểm tra huy hiệu yêu cầu
            if (voucher.RequiredBadgeID.HasValue)
            {
                bool hasBadge = await _context.BadgeCustomers
                    .AnyAsync(bc => bc.CustomerID == customerId && bc.BadgeID == voucher.RequiredBadgeID.Value);

                if (!hasBadge)
                {
                    invalid.ErrorMessage = "Bạn chưa đạt được huy hiệu yêu cầu để dùng voucher này.";
                    return invalid;
                }
            }

            // 10. Kiểm tra điều kiện khách hàng mới
            if (voucher.IsForNewUser)
            {
                int orderCount = await _context.Orders.CountAsync(o => o.CustomerID == customerId);
                if (orderCount > 0)
                {
                    invalid.ErrorMessage = "Voucher này chỉ dành cho khách hàng đặt đơn lần đầu.";
                    return invalid;
                }
            }

            // 11. Kiểm tra scope (Category) — nếu có VoucherCategory thì đơn hàng phải có ít nhất 1 sản phẩm thuộc category đó
            var requiredCategoryIds = voucher.VoucherCategories.Select(vc => vc.CategoryID).ToList();
            if (requiredCategoryIds.Any())
            {
                bool scopeMatch = bookCategoryIds.Any(cid => requiredCategoryIds.Contains(cid));
                if (!scopeMatch)
                {
                    invalid.ErrorMessage = "Không có sản phẩm nào trong giỏ hàng thuộc danh mục áp dụng của voucher này.";
                    return invalid;
                }
            }

            // 12. Tính giá trị giảm giá
            decimal discountAmount;
            if (voucher.DiscountType == VoucherDiscountType.Fixed)
            {
                discountAmount = voucher.DiscountValue;
            }
            else
            {
                discountAmount = orderSubTotal * voucher.DiscountValue / 100;
                if (discountAmount > voucher.MaxDiscountAmount)
                    discountAmount = voucher.MaxDiscountAmount;
            }

            return new VoucherValidationResultDTO
            {
                IsValid = true,
                DiscountAmount = discountAmount,
                VoucherID = voucher.VoucherID,
                VoucherCode = voucher.VoucherCode
            };
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
            var order = await _context.Orders.FindAsync(orderId);
            if (order?.VoucherID == null) return;

            var voucher = await _context.Vouchers.FindAsync(order.VoucherID.Value);
            if (voucher == null || !voucher.IsAutoRefundable) return;

            var customerVoucher = await _context.CustomerVouchers
                .FirstOrDefaultAsync(cv => cv.OrderID == orderId
                                        && cv.VoucherID == order.VoucherID.Value);

            if (customerVoucher == null) return;

            // Hoàn trả: reset về chưa dùng
            customerVoucher.IsUsed = false;
            customerVoucher.UsedAt = SentinelDate;
            customerVoucher.OrderID = null;

            // Giảm UsedCount
            if (voucher.UsedCount > 0) voucher.UsedCount--;

            await _context.SaveChangesAsync();
        }

        private async Task AutoUpdateVoucherStatusesAsync()
        {
            var now = DateTime.UtcNow;

            // 1. Scheduled -> Active: Nếu đã đến StartDate nhưng chưa quá EndDate và đang Scheduled
            var toActive = await _context.Vouchers
                .Where(v => v.StatusVoucher == VoucherStatus.Scheduled && v.StartDate <= now && v.EndDate > now)
                .ToListAsync();

            foreach (var v in toActive)
            {
                v.StatusVoucher = VoucherStatus.Active;
            }

            // 2. Active/Scheduled/Paused/Draft -> Ended: Nếu đã quá EndDate (ngoại trừ các voucher đã kết thúc)
            var toEnded = await _context.Vouchers
                .Where(v => v.StatusVoucher != VoucherStatus.Ended && v.EndDate <= now)
                .ToListAsync();

            foreach (var v in toEnded)
            {
                v.StatusVoucher = VoucherStatus.Ended;
            }

            // 3. Active -> Scheduled: Nếu ngày bắt đầu ở tương lai và đang là Active
            var toScheduled = await _context.Vouchers
                .Where(v => v.StatusVoucher == VoucherStatus.Active && v.StartDate > now)
                .ToListAsync();

            foreach (var v in toScheduled)
            {
                v.StatusVoucher = VoucherStatus.Scheduled;
            }

            if (toActive.Any() || toEnded.Any() || toScheduled.Any())
            {
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
            ApplicableCategoryIDs = v.VoucherCategories?.Select(vc => vc.CategoryID).ToList() ?? new()
        };
    }
}
