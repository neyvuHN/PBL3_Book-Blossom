using BookBlossom.Core.Entities;
using BookBlossom.Core.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookBlossom.Infrastructure.Data.Configurations
{
    // === CẤU HÌNH BẢNG Voucher.Voucher ===
    public class VoucherConfiguration : IEntityTypeConfiguration<Voucher>
    {
        public void Configure(EntityTypeBuilder<Voucher> builder)
        {
            builder.ToTable("Voucher", "Voucher");

            builder.HasKey(v => v.VoucherID);

            builder.Property(v => v.VoucherName)
                   .HasMaxLength(255)
                   .IsRequired();

            builder.Property(v => v.VoucherCode)
                   .HasMaxLength(50)
                   .IsUnicode(false)
                   .IsRequired();

            builder.HasIndex(v => v.VoucherCode)
                   .IsUnique()
                   .HasDatabaseName("UQ_Voucher_VoucherCode");

            // DiscountType lưu dạng string trong DB ("Fixed" / "Percentage")
            builder.Property(v => v.DiscountType)
                   .HasConversion<string>()
                   .HasMaxLength(20)
                   .IsUnicode(false)
                   .IsRequired();

            builder.Property(v => v.DiscountValue)
                   .HasColumnType("decimal(18,2)")
                   .IsRequired();

            builder.Property(v => v.MaxDiscountAmount)
                   .HasColumnType("decimal(18,2)")
                   .IsRequired();

            builder.Property(v => v.MinOrderValue)
                   .HasColumnType("decimal(18,2)")
                   .IsRequired();

            builder.Property(v => v.TotalLimit).IsRequired();
            builder.Property(v => v.UsedCount).IsRequired();

            builder.Property(v => v.StartDate).HasColumnType("datetime").IsRequired();
            builder.Property(v => v.EndDate).HasColumnType("datetime").IsRequired();

            // StatusVoucher lưu dạng tinyint trong DB
            builder.Property(v => v.StatusVoucher)
                   .HasColumnType("tinyint")
                   .IsRequired();

            // MinPlan lưu dạng tinyint trong DB
            builder.Property(v => v.MinPlan)
                   .HasColumnType("tinyint")
                   .IsRequired()
                   .HasDefaultValue(SubscriptionType.Free);

            builder.Property(v => v.MinReputationRequired).IsRequired();
            builder.Property(v => v.MembershipRankRequired).IsRequired();
            builder.Property(v => v.IsForNewUser).IsRequired();
            builder.Property(v => v.IsStackable).IsRequired();
            builder.Property(v => v.IsAutoRefundable).IsRequired();
            builder.Property(v => v.MaxUsagePerUser).IsRequired();

            // RequiredBadgeID Nullable - không tạo FK constraint (tránh conflict với Badge schema)
            builder.Property(v => v.RequiredBadgeID);

            // FK sang Badge (nullable, không cascade để tránh circular delete)
            builder.HasOne(v => v.RequiredBadge)
                   .WithMany()
                   .HasForeignKey(v => v.RequiredBadgeID)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }

    // === CẤU HÌNH BẢNG Voucher.CustomerVoucher ===
    public class CustomerVoucherConfiguration : IEntityTypeConfiguration<CustomerVoucher>
    {
        public void Configure(EntityTypeBuilder<CustomerVoucher> builder)
        {
            builder.ToTable("CustomerVoucher", "Voucher");

            // Composite PK: {CustomerID, VoucherID}
            builder.HasKey(cv => new { cv.CustomerID, cv.VoucherID });

            builder.Property(cv => cv.IsUsed).IsRequired();

            // Sentinel: 2099-12-31 khi chưa dùng
            builder.Property(cv => cv.UsedAt)
                   .HasColumnType("datetime")
                   .IsRequired();

            builder.Property(cv => cv.OrderID);

            // FK: CustomerVoucher -> CustomerDetail
            builder.HasOne(cv => cv.CustomerDetail)
                   .WithMany(cd => cd.CustomerVouchers)
                   .HasForeignKey(cv => cv.CustomerID)
                   .OnDelete(DeleteBehavior.Restrict);

            // FK: CustomerVoucher -> Voucher
            builder.HasOne(cv => cv.Voucher)
                   .WithMany(v => v.CustomerVouchers)
                   .HasForeignKey(cv => cv.VoucherID)
                   .OnDelete(DeleteBehavior.Cascade);

            // FK: CustomerVoucher -> Orders (nullable)
            builder.HasOne(cv => cv.Order)
                   .WithMany()
                   .HasForeignKey(cv => cv.OrderID)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }

    // === CẤU HÌNH BẢNG Voucher.VoucherCategory ===
    public class VoucherCategoryConfiguration : IEntityTypeConfiguration<VoucherCategory>
    {
        public void Configure(EntityTypeBuilder<VoucherCategory> builder)
        {
            builder.ToTable("VoucherCategory", "Voucher");

            // Composite PK: {VoucherID, CategoryID}
            builder.HasKey(vc => new { vc.VoucherID, vc.CategoryID });

            // FK: VoucherCategory -> Voucher
            builder.HasOne(vc => vc.Voucher)
                   .WithMany(v => v.VoucherCategories)
                   .HasForeignKey(vc => vc.VoucherID)
                   .OnDelete(DeleteBehavior.Cascade);

            // FK: VoucherCategory -> Category
            builder.HasOne(vc => vc.Category)
                   .WithMany()
                   .HasForeignKey(vc => vc.CategoryID)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }

    // === CẤU HÌNH BẢNG Voucher.VoucherBook ===
    public class VoucherBookConfiguration : IEntityTypeConfiguration<VoucherBook>
    {
        public void Configure(EntityTypeBuilder<VoucherBook> builder)
        {
            builder.ToTable("VoucherBook", "Voucher");

            // Composite PK: {VoucherID, BookID}
            builder.HasKey(vb => new { vb.VoucherID, vb.BookID });

            // FK: VoucherBook -> Voucher
            builder.HasOne(vb => vb.Voucher)
                   .WithMany(v => v.VoucherBooks)
                   .HasForeignKey(vb => vb.VoucherID)
                   .OnDelete(DeleteBehavior.Cascade);

            // FK: VoucherBook -> RealBook
            builder.HasOne(vb => vb.Book)
                   .WithMany()
                   .HasForeignKey(vb => vb.BookID)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
