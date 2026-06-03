using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using BookBlossom.Core.Enums;
using BookBlossom.Infrastructure.Data;

namespace BookBlossom.Web.Middlewares
{
    public class OnboardingRedirectMiddleware
    {
        private readonly RequestDelegate _next;

        public OnboardingRedirectMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, ApplicationDbContext dbContext)
        {
            var path = context.Request.Path.Value?.ToLower() ?? "";

            // Ignore static assets, APIs, Hubs, Home page, and authentication-related paths
            if (path == "/" ||
                path == "/home" ||
                path == "/home/index" ||
                path.StartsWith("/api") || 
                path.StartsWith("/hubs") ||
                path.StartsWith("/css") || 
                path.StartsWith("/js") || 
                path.StartsWith("/images") || 
                path.StartsWith("/lib") ||
                path.EndsWith(".ico") ||
                path.EndsWith(".png") ||
                path.EndsWith(".jpg") ||
                path.EndsWith(".gif") ||
                path.Contains("/auth/interestselection") ||
                path.Contains("/auth/logout") ||
                path.Contains("/auth/login") ||
                path.Contains("/auth/register"))
            {
                await _next(context);
                return;
            }

            // Check if user is authenticated and is a Customer
            if (context.User.Identity != null && context.User.Identity.IsAuthenticated)
            {
                var roleClaim = context.User.FindFirst(ClaimTypes.Role)?.Value;
                if (roleClaim == UserRole.Customer.ToString() || roleClaim == "1")
                {
                    var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    if (long.TryParse(userIdClaim, out long userId))
                    {
                        var customer = await dbContext.CustomerDetails
                            .AsNoTracking()
                            .FirstOrDefaultAsync(cd => cd.CustomerID == userId);

                        // If customer details don't exist or onboarding isn't completed, redirect
                        if (customer == null || !customer.IsOnboardingCompleted)
                        {
                            context.Response.Redirect("/Auth/InterestSelection");
                            return;
                        }
                    }
                }
            }

            await _next(context);
        }
    }
}
