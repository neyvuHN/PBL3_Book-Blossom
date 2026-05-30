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

            // 2. Nếu không có ở Header, thử lấy từ Cookie
            if (string.IsNullOrEmpty(guestIdHeader))
            {
                guestIdHeader = context.Request.Cookies["X-Guest-Id"];
            }

            if (!string.IsNullOrEmpty(guestIdHeader) && Guid.TryParse(guestIdHeader, out var guestId))
            {
                using (var scope = context.RequestServices.CreateScope())
                {
                    var guestService = scope.ServiceProvider.GetRequiredService<IGuestService>();
                    
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