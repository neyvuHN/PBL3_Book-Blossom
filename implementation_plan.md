# Kế hoạch kết nối Frontend và Backend dự án BookBlossom

Hiện tại Frontend (FE) và Backend (BE) đang tách biệt, các Controller ở FE (như `AdminController`, `OrderController`...) đang sử dụng dữ liệu tĩnh (mock data). Mục tiêu của kế hoạch này là kết nối các Controller với các Service ở tầng `Infrastructure` để lấy dữ liệu thực tế từ Database, đồng thời hoàn thiện các tính năng BE còn thiếu theo mức độ ưu tiên.

## User Review Required

> [!IMPORTANT]
> Bạn vui lòng xem qua lộ trình kết nối và mức độ ưu tiên của các module BE còn thiếu dưới đây. Nếu bạn đồng ý với thứ tự này, hãy phê duyệt để chúng ta bắt đầu tiến hành kết nối từng bước.

## Đánh giá mức độ ưu tiên các module BE còn thiếu

Dựa trên luồng nghiệp vụ của một hệ thống thương mại điện tử kết hợp cộng đồng:

1. **API Tính điểm uy tín (Reputation System)**: **Quan trọng nhất (Ưu tiên 1)**. Hệ thống phân quyền và hành vi người dùng (như chặn COD, cấm bình luận) phụ thuộc rất nhiều vào điểm này. Cần làm sớm để tích hợp vào các luồng tạo tài khoản, mua hàng, đánh giá.
2. **Thống kê Dashboard**: **Ưu tiên 2**. Cần thiết cho Ban Quản Trị (Admin) để theo dõi tình hình kinh doanh, doanh thu, đơn hàng.
3. **Voucher**: **Ưu tiên 3**. Phục vụ cho marketing và kích cầu mua sắm. Cần làm trước hoặc cùng lúc với lúc hoàn thiện luồng Thanh toán/Cart.
4. **Audit Logs**: **Ưu tiên 4**. Tính năng giám sát bảo mật hệ thống, ghi nhận thao tác của Admin/Nhân viên. Có thể thực hiện cuối cùng sau khi các nghiệp vụ chính đã chạy ổn định.

---

## Lộ trình triển khai (Proposed Changes)

Quá trình kết nối và phát triển sẽ được chia thành các Giai đoạn (Phases) từ quan trọng nhất đến ít quan trọng nhất:

### Giai đoạn 1: Nền tảng, Xác thực & Phân quyền (Authentication & Authorization)
Kết nối các tính năng cơ bản nhất để người dùng có thể đăng nhập và định danh.
- Thiết lập Dependency Injection (DI) cho các Services và DbContext trong `Program.cs`.
- Kết nối `AuthController` (Đăng nhập, Đăng ký, Đăng xuất) với BE Services.
- Cập nhật thông tin User, Role trong `ProfileController`.
- **[BE Task]**: Xây dựng logic khởi tạo tài khoản và cộng điểm uy tín mặc định.

### Giai đoạn 2: Quản lý Sản phẩm (Catalog & Inventory)
Kết nối dữ liệu hiển thị trên trang chủ và trang quản lý kho của Admin.
- Thay thế mock data trong `AdminController.Inventory()` bằng dữ liệu thật (`BookService`, `CategoryService`).
- Kết nối `BookController`, `RealBookController`, `BlindBookController`, `CategoryController`, `InventoryController`.

### Giai đoạn 3: Luồng Đặt hàng & Hệ thống Điểm uy tín (Order & Reputation)
Đây là nghiệp vụ cốt lõi của ứng dụng.
- Kết nối `CartController` và `OrderController`.
- Xử lý các trạng thái đơn hàng (Pending, To Ship, In Transit...) ở cả phía Khách hàng và Admin (`AdminController.Orders`).
- **[BE Task - Quan trọng nhất]**: Viết và Test API chức năng tự động cộng trừ điểm uy tín theo các action:
  - Nhận hàng thành công (COD) / Thanh toán trước (Online).
  - Hủy đơn sau khi Shop đóng gói / "Bom" hàng.
- Tích hợp API Điểm uy tín vào luồng cập nhật trạng thái đơn hàng (`OrderService`).

### Giai đoạn 4: Cộng đồng & Đánh giá (Community & Interactions)
Kết nối các tính năng tương tác của người dùng.
- Kết nối `CommunityController`, `ThreadController`, `ReviewController`.
- **[BE Task - Tiếp tục]**: Tích hợp API Điểm uy tín cho các hành động:
  - Review chất lượng đạt >= 5 like.
  - Bị Report đúng (Spam/Thô tục) / Bị xóa/ẩn bài review, thread.

### Giai đoạn 5: Dashboard & Thống kê (Analytics)
- **[BE Task]**: Xây dựng các API/Service thống kê doanh thu, số lượng đơn hàng, người dùng, xu hướng mua hàng.
- Kết nối `AdminController.Dashboard()` để hiển thị các biểu đồ và số liệu thực tế thay vì mock data.

### Giai đoạn 6: Voucher & Giỏ hàng nâng cao (Promotions)
- **[BE Task]**: Xây dựng cấu trúc Database và logic xử lý cho Voucher (Điều kiện áp dụng, giảm giá %, giảm giá trực tiếp).
- Kết nối Voucher vào `CartController` để áp dụng mã giảm giá khi Checkout.
- Kết nối `AdminController.Vouchers()` để quản lý danh sách Voucher.

### Giai đoạn 7: Audit Logs & Tiện ích (Security & Utilities)
- **[BE Task]**: Xây dựng Middleware hoặc Interceptor để tự động ghi log các thay đổi (Thêm, Sửa, Xóa) của các User/Admin vào bảng `AuditLog`.
- Kết nối `AdminController.SystemLogs()` để hiển thị lịch sử thao tác.
- Kết nối các tính năng phụ còn lại như `WishlistController`, `NotificationController`.

---

## Verification Plan

### Automated Tests
- Chạy unit tests cho các Service ở tầng `BookBlossom.Infrastructure` (nếu có).
- Postman / Swagger: Gửi request giả lập để test API **Tính điểm uy tín** cho từng action độc lập (Bom hàng, Nhận hàng, Bị xóa bài...) nhằm đảm bảo điểm số thay đổi chính xác.

### Manual Verification
- Chạy ứng dụng (`dotnet run`) và thực hiện flow toàn diện: Đăng nhập -> Thêm giỏ hàng -> Đặt hàng -> Admin duyệt đơn -> Giao thành công.
- Kiểm tra xem điểm uy tín của Buyer có tăng lên đúng theo cấu hình hay không.
- Kiểm tra Dashboard của Admin xem số lượng đơn hàng và doanh thu có được cập nhật theo thời gian thực (hoặc sau khi reload) hay không.
