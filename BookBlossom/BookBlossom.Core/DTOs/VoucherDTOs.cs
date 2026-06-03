using System;
using System.Collections.Generic;
using BookBlossom.Core.Enums;

namespace BookBlossom.Core.DTOs
{
    // DTO trả về chi tiết 1 voucher
    public class VoucherDTO
    {
        public long VoucherID { get; set; }
        public string VoucherName { get; set; } = string.Empty;
        public string VoucherCode { get; set; } = string.Empty;
        public VoucherDiscountType DiscountType { get; set; }
        public decimal DiscountValue { get; set; }
        public decimal MaxDiscountAmount { get; set; }
        public decimal MinOrderValue { get; set; }
        public int TotalLimit { get; set; }
        public int UsedCount { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public VoucherStatus StatusVoucher { get; set; }
        public int MinReputationRequired { get; set; }
        public int MembershipRankRequired { get; set; }
        public SubscriptionType MinPlan { get; set; }
        public bool IsForNewUser { get; set; }
        public long? RequiredBadgeID { get; set; }
        public bool IsStackable { get; set; }
        public bool IsAutoRefundable { get; set; }
        public int MaxUsagePerUser { get; set; }
        public List<long> ApplicableCategoryIDs { get; set; } = new();
    }

    // DTO tạo mới voucher (Marketing/Admin)
    public class CreateVoucherDTO
    {
        public string VoucherName { get; set; } = string.Empty;
        public string VoucherCode { get; set; } = string.Empty;
        public VoucherDiscountType DiscountType { get; set; }
        public decimal DiscountValue { get; set; }
        public decimal MaxDiscountAmount { get; set; }
        public decimal MinOrderValue { get; set; }
        public int TotalLimit { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int MinReputationRequired { get; set; } = 0;
        public int MembershipRankRequired { get; set; } = 0;
        public SubscriptionType MinPlan { get; set; } = SubscriptionType.Free;
        public bool IsForNewUser { get; set; } = false;
        public long? RequiredBadgeID { get; set; }
        public bool IsStackable { get; set; } = false;
        public bool IsAutoRefundable { get; set; } = false;
        public int MaxUsagePerUser { get; set; } = 1;
        public VoucherStatus StatusVoucher { get; set; } = VoucherStatus.Draft;
        // Nếu rỗng = áp dụng toàn sàn
        public List<long> ApplicableCategoryIDs { get; set; } = new();
    }

    // DTO cập nhật voucher
    public class UpdateVoucherDTO
    {
        public string? VoucherName { get; set; }
        public VoucherDiscountType? DiscountType { get; set; }
        public decimal? DiscountValue { get; set; }
        public decimal? MaxDiscountAmount { get; set; }
        public decimal? MinOrderValue { get; set; }
        public int? TotalLimit { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public VoucherStatus? StatusVoucher { get; set; }
        public int? MinReputationRequired { get; set; }
        public int? MembershipRankRequired { get; set; }
        public SubscriptionType? MinPlan { get; set; }
        public bool? IsForNewUser { get; set; }
        public long? RequiredBadgeID { get; set; }
        public bool? IsStackable { get; set; }
        public bool? IsAutoRefundable { get; set; }
        public int? MaxUsagePerUser { get; set; }
        public List<long>? ApplicableCategoryIDs { get; set; }
    }

    // Kết quả sau khi validate và áp voucher tại checkout
    public class VoucherValidationResultDTO
    {
        public bool IsValid { get; set; }
        public string? ErrorMessage { get; set; }
        public decimal DiscountAmount { get; set; }
        public long? VoucherID { get; set; }
        public string? VoucherCode { get; set; }
    }

    // Thống kê sử dụng voucher (Usage Rate & ROI cho Marketing)
    public class VoucherUsageStatsDTO
    {
        public long VoucherID { get; set; }
        public string VoucherCode { get; set; } = string.Empty;
        public int TotalLimit { get; set; }
        public int UsedCount { get; set; }
        public double UsageRate { get; set; } // UsedCount / TotalLimit * 100
        public decimal TotalDiscountGranted { get; set; }
        public decimal TotalRevenueGenerated { get; set; }
        public decimal ROI { get; set; } // TotalRevenueGenerated / TotalDiscountGranted
    }

    // DTO hiển thị voucher trong ví của khách hàng
    public class CustomerVoucherDTO
    {
        public long VoucherID { get; set; }
        public string VoucherName { get; set; } = string.Empty;
        public string VoucherCode { get; set; } = string.Empty;
        public VoucherDiscountType DiscountType { get; set; }
        public decimal DiscountValue { get; set; }
        public decimal MaxDiscountAmount { get; set; }
        public decimal MinOrderValue { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsUsed { get; set; }
        public long? OrderID { get; set; }
    }
}
