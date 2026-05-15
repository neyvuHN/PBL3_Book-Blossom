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
            // Lấy ID và Token từ header
            var guestIdHeader = context.Request.Headers["X-Guest-Id"].ToString();
            var guestTokenHeader = context.Request.Headers["X-Guest-Token"].ToString();

            if (!string.IsNullOrEmpty(guestIdHeader) && Guid.TryParse(guestIdHeader, out var guestId) && !string.IsNullOrEmpty(guestTokenHeader))
            {
                // Dùng service scope để lấy service (vì middleware là singleton, IGuestService là scoped)
                using (var scope = context.RequestServices.CreateScope())
                {
                    var guestService = scope.ServiceProvider.GetRequiredService<IGuestService>();
                    var isValid = await guestService.ValidateGuestSessionAsync(guestId, guestTokenHeader);

                    if (isValid)
                    {
                        // Lưu thông tin Guest vào Items để Controller có thể lấy dùng nhanh
                        context.Items["GuestID"] = guestId;
                    }
                }
            }

            await _next(context);
        }
    }
}
