using System;
using System.Collections.Generic;
using BookBlossom.Core.Enums;

namespace BookBlossom.Core.Entities
{
    // Ánh xạ bảng Voucher.Voucher
    public class Voucher
    {
        public long VoucherID { get; set; }
        public string VoucherName { get; set; } = string.Empty;
        public string VoucherCode { get; set; } = string.Empty;

        // Được convert về string "Fixed" / "Percentage" trong DB
        public VoucherDiscountType DiscountType { get; set; }
        public decimal DiscountValue { get; set; }
        public decimal MaxDiscountAmount { get; set; }
        public decimal MinOrderValue { get; set; }

        public int TotalLimit { get; set; }
        public int UsedCount { get; set; } = 0;

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        // Được lưu dạng tinyint trong DB
        public VoucherStatus StatusVoucher { get; set; } = VoucherStatus.Draft;

        // Gói hội viên tối thiểu để dùng voucher
        public SubscriptionType MinPlan { get; set; } = SubscriptionType.Free;

        // Điểm uy tín tối thiểu để dùng voucher
        public int MinReputationRequired { get; set; } = 0;

        // So khớp với RankType của bảng MembershipRank (0 = không yêu cầu)
        public int MembershipRankRequired { get; set; } = 0;

        public bool IsForNewUser { get; set; } = false;

        // Badge bắt buộc (null = không yêu cầu)
        public long? RequiredBadgeID { get; set; }

        public bool IsStackable { get; set; } = false;
        public bool IsAutoRefundable { get; set; } = false;

        // Số lần tối đa mỗi user được dùng voucher này
        public int MaxUsagePerUser { get; set; } = 1;

        // Navigation Properties
        public virtual Badge? RequiredBadge { get; set; }
        public virtual ICollection<CustomerVoucher> CustomerVouchers { get; set; } = new List<CustomerVoucher>();
        public virtual ICollection<VoucherCategory> VoucherCategories { get; set; } = new List<VoucherCategory>();
        public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
        public virtual ICollection<VoucherBook> VoucherBooks { get; set; } = new List<VoucherBook>();
    }

    // Ánh xạ bảng Voucher.CustomerVoucher — ví voucher của từng khách hàng
    public class CustomerVoucher
    {
        public long CustomerID { get; set; }
        public long VoucherID { get; set; }

        // Null khi chưa dùng
        public long? OrderID { get; set; }

        public bool IsUsed { get; set; } = false;

        // Sentinel: 2099-12-31 khi chưa dùng, DateTime.UtcNow khi đã dùng
        public DateTime UsedAt { get; set; } = new DateTime(2099, 12, 31);

        // Navigation Properties
        public virtual CustomerDetail CustomerDetail { get; set; } = null!;
        public virtual Voucher Voucher { get; set; } = null!;
        public virtual Order? Order { get; set; }
    }

    // Ánh xạ bảng Voucher.VoucherCategory — danh mục áp dụng
    public class VoucherCategory
    {
        public long VoucherID { get; set; }
        public long CategoryID { get; set; }

        // Navigation Properties
        public virtual Voucher Voucher { get; set; } = null!;
        public virtual Category Category { get; set; } = null!;
    }

    // Ánh xạ bảng Voucher.VoucherBook — sách áp dụng
    public class VoucherBook
    {
        public long VoucherID { get; set; }
        public long BookID { get; set; }

        // Navigation Properties
        public virtual Voucher Voucher { get; set; } = null!;
        public virtual RealBook Book { get; set; } = null!;
    }
}
