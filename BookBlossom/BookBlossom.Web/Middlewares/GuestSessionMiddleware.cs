using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using BookBlossom.Core.Interfaces.Services;

namespace BookBlossom.Web.Middlewares
{
    public class GuestSessionMiddleware
    {
        private readonly RequestDelegate _next;

        public GuestSessionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // 1. Chỉ cần lấy duy nhất X-Guest-Id từ Header
            var guestIdHeader = context.Request.Headers["X-Guest-Id"].ToString();

            if (!string.IsNullOrEmpty(guestIdHeader) && Guid.TryParse(guestIdHeader, out var guestId))
            {
                using (var scope = context.RequestServices.CreateScope())
                {
                    var guestService = scope.ServiceProvider.GetRequiredService<IGuestService>();
                    
                    // 2. 🟢 THAY ĐỔI: Sử dụng một hàm check tồn tại của Id, không check kèm Token nữa.
                    // Nếu trong IGuestService của bạn CHƯA CÓ hàm IsGuestExistsAsync(guestId),
                    // bạn có thể tạm thời đổi thành: var isValid = true; (để test nhanh trên Swagger).
                    // Hoặc triển khai hàm kiểm tra sự tồn tại của GuestId trong DB như dưới đây:
                    var isValid = await guestService.IsGuestExistsAsync(guestId); 

                    if (isValid)
                    {
                        // Lưu thông tin Guest vào Items để TindbookController bốc ra xài
                        context.Items["GuestID"] = guestId;
                    }
                }
            }

            await _next(context);
        }
    }
}