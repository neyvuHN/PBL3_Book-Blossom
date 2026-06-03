using BookBlossom.Core.Interfaces.Services;
using BookBlossom.Core.Interfaces;
using BookBlossom.Infrastructure.BackgroundJobs;
using BookBlossom.Infrastructure.Data;
using BookBlossom.Infrastructure.Services;
using BookBlossom.Web.Middlewares;
using BookBlossom.Web.Hubs;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;


using System.Text;
using System.Security.Claims;
using BookBlossom.Core.Enums;
using OpenApiModels = Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Đăng ký ApplicationDbContext sử dụng SQL Server
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Đăng ký các Service khác (Đảm bảo đã có các dòng này)
builder.Services.AddScoped<IOTPService, OTPService>();
builder.Services.AddHttpClient<ISMSService, EsmsSmsService>();
builder.Services.AddMemoryCache();

// Đăng ký Background Job dọn dẹp OTP
builder.Services.AddHostedService<OtpCleanupJob>();

// Đăng ký AuthService
builder.Services.AddScoped<IAuthService, AuthService>();

// Đăng ký GuestService
builder.Services.AddScoped<IGuestService, GuestService>();

// Đăng ký UserService
builder.Services.AddScoped<IUserService, UserService>();

// Đăng ký Service Module
builder.Services.AddScoped<IServicePackageService, ServicePackageService>();
builder.Services.AddHostedService<SubscriptionExpiryJob>();

// Đăng ký Background Job tự động hủy đơn sau 48h chưa xác nhận
builder.Services.AddHostedService<OrderAutoCancelService>();

// Đăng ký IOnboardingService
builder.Services.AddScoped<IOnboardingService, OnboardingService>();

// Đăng ký ICategoryService
builder.Services.AddScoped<ICategoryService, CategoryService>();

// Đăng ký IInventoryService
builder.Services.AddScoped<IInventoryService, InventoryService>();

// Đăng ký ITindbookService
builder.Services.AddScoped<ITindbookService, TindbookService>();

// Đăng ký IRealBookService
builder.Services.AddScoped<IRealBookService, RealBookService>();

// Đăng ký ICartService & IWishlistService
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<IWishlistService, WishlistService>();

// Đăng ký IBlindBookService
builder.Services.AddScoped<IBlindBookService, BlindBookService>();

// Đăng ký IOrderService
builder.Services.AddScoped<IOrderService, OrderService>(); 

// Đăng ký IReturnService
builder.Services.AddScoped<IReturnService, ReturnService>(); 

// Đăng ký IThreadService
builder.Services.AddScoped<IThreadService, ThreadService>(); 

// Đăng ký Notification Services
builder.Services.AddScoped<INotificationPublisher, BookBlossom.Web.Hubs.NotificationPublisher>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddSignalR();

// Đăng ký IReviewService
builder.Services.AddScoped<IReviewService, ReviewService>();

// Đăng ký IGamificationService
builder.Services.AddScoped<IGamificationService, GamificationService>();

// Đăng ký IVoucherService
builder.Services.AddScoped<IVoucherService, VoucherService>();

// Đăng ký IReputationService
builder.Services.AddScoped<IReputationService, ReputationService>();

// Đăng ký IStatisticsService
builder.Services.AddScoped<IStatisticsService, StatisticsService>();

// Đăng ký IAuditService
builder.Services.AddScoped<IAuditService, AuditService>();

// Đăng ký IVnPayService
builder.Services.AddScoped<IVnPayService, VnPayService>();

// Cấu hình JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? "Nuocmatemroitrochoiketthuc_BookBlossom_Security_Key_2026")),

            RoleClaimType = ClaimTypes.Role
        };
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                // Read token from cookie for Razor pages
                if (context.Request.Cookies.ContainsKey("AuthToken"))
                {
                    context.Token = context.Request.Cookies["AuthToken"];
                }

                var accessToken = context.Request.Query["access_token"];

                // If the request is for our hub...
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) &&
                    (path.StartsWithSegments("/hubs/notification")))
                {
                    // Read the token out of the query string
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            },
            OnChallenge = async context =>
            {
                context.HandleResponse();
                context.Response.StatusCode = 401;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(new { message = "Hãy đăng nhập để thực hiện chức năng" });
            },
            OnForbidden = async context =>
            {
                context.Response.StatusCode = 403;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(new { message = "Bạn không có quyền thực hiện chức năng này" });
            }
        };
    });

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiModels.OpenApiInfo
    {
        Title = "BookBlossom API",
        Version = "v1"
    });

    // 1. Cấu hình cho USER (Dùng JWT Token - GIỮ NGUYÊN CODE CŨ CỦA BẠN)
    var securityScheme = new OpenApiModels.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "Nhập token JWT của bạn",
        In = OpenApiModels.ParameterLocation.Header,
        Type = OpenApiModels.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Reference = new OpenApiModels.OpenApiReference
        {
            Id = "Bearer",
            Type = OpenApiModels.ReferenceType.SecurityScheme
        }
    };
    c.AddSecurityDefinition("Bearer", securityScheme);
    c.AddSecurityRequirement(new OpenApiModels.OpenApiSecurityRequirement
    {
        { securityScheme, Array.Empty<string>() }
    });
    // X-Guest-Id
    var guestIdScheme = new OpenApiModels.OpenApiSecurityScheme
    {
        Name = "X-Guest-Id",
        In = OpenApiModels.ParameterLocation.Header,
        Type = OpenApiModels.SecuritySchemeType.ApiKey,
        Reference = new OpenApiModels.OpenApiReference
        {
            Id = "GuestId",
            Type = OpenApiModels.ReferenceType.SecurityScheme
        }
    };
    c.AddSecurityDefinition("GuestId", guestIdScheme);

    c.AddSecurityRequirement(new OpenApiModels.OpenApiSecurityRequirement
    {
        { guestIdScheme, Array.Empty<string>() }
    });
});

builder.Services.AddControllersWithViews();
builder.Services.AddAuthorization(options =>
{
    // Hằng số cho AccountStatus.Active
    var activeStatus = ((int)AccountStatus.Active).ToString();

    // 1. Policy cho Admin (Toàn quyền) - Chấp nhận cả số enum lẫn chữ cứng
    options.AddPolicy("AdminOnly", policy => 
    {
        policy.RequireClaim(ClaimTypes.Role, ((int)UserRole.Admin).ToString(), "Admin");
        policy.RequireClaim("AccountStatus", activeStatus, "1");
    });
            
    // 2. Policy cho Khách hàng đã định danh
    options.AddPolicy("CustomerOnly", policy =>
    {
        policy.RequireClaim(ClaimTypes.Role, ((int)UserRole.Customer).ToString(), "Customer");
        policy.RequireClaim("AccountStatus", activeStatus, "1");
    });

    // 3. Policy cho RequireSystemAdmin (quyền cao nhất)
    options.AddPolicy("RequireSystemAdmin", policy =>
    {
        policy.RequireClaim(ClaimTypes.Role, ((int)UserRole.Admin).ToString(), "Admin");
        policy.RequireClaim("AccountStatus", activeStatus, "1");
    });
});
var app = builder.Build();

// Seed dữ liệu mặc định hệ thống
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<BookBlossom.Infrastructure.Data.ApplicationDbContext>();
        await BookBlossom.Infrastructure.Data.SeedData.InitializeAsync(context);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Lỗi khi seed dữ liệu.");
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseMiddleware<GuestSessionMiddleware>();

app.UseAuthentication(); // Thêm dòng này trước UseAuthorization
app.UseAuthorization();

app.MapStaticAssets();
app.MapHub<NotificationHub>("/hubs/notification");
app.MapControllers();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
