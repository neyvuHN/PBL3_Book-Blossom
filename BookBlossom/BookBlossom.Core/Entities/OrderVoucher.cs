using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BookBlossom.Core.Entities
{
    public class OrderVoucher
    {
        [Key, Column(Order = 0)]
        public long OrderID { get; set; }

        [Key, Column(Order = 1)]
        public long VoucherID { get; set; }

        public decimal DiscountAmount { get; set; }

        public virtual Order? Order { get; set; }
        public virtual Voucher? Voucher { get; set; }
    }
}
