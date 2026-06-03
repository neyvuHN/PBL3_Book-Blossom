using System;

namespace BookBlossom.Core.DTOs.Book
{
    public class RealBookDTO
    {
        // 1. Thông tin định danh (Bắt buộc phải có ID để FE làm các nút Xem chi tiết/Sửa/Xóa)
        public long BookID { get; set; }          

        // 2. Thông tin danh mục liên kết
        public long CategoryID { get; set; }
        public string CategoryName { get; set; } = string.Empty; // Hiển thị chữ "Văn Học", "Kỹ Năng" ra màn hình thay vì mỗi ID số số

        // 3. Thông tin chi tiết của sách
        public string Title { get; set; } = string.Empty;
        public string? Publisher { get; set; }
        public string ISBN { get; set; } = string.Empty;
        public int PublishYear { get; set; }
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public decimal Weight { get; set; }

        // 4. Đường dẫn file đọc thử PDF trên Server để FE gắn link cho khách bấm đọc
        public string? SampleFilePath { get; set; } 

        // 5. Trạng thái quản trị kho hàng và kinh doanh công khai
        public int UnitsInStock { get; set; }
        public int ReservedQuantity { get; set; }    // Số lượng đang bị giữ chân bởi đơn hàng chờ xử lý
        public bool IsContinued { get; set; }       // Trạng thái kinh doanh (Xóa mềm)

        public string? Authors { get; set; }
        public int SoldCount { get; set; }
    }
}