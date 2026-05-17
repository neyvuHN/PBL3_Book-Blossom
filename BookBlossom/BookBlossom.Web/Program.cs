using BookBlossom.Core.Interfaces.Services;
using BookBlossom.Core.Interfaces;
using BookBlossom.Infrastructure.BackgroundJobs;
using BookBlossom.Infrastructure.Data;
using BookBlossom.Infrastructure.Services;
using BookBlossom.Web.Middlewares;
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
builder.Services.AddScoped<ISMSService, MockSmsService>();
builder.Services.AddMemoryCache();

// Đăng ký Background Job dọn dẹp OTP
builder.Services.AddHostedService<OtpCleanupJob>();

// Đăng ký AuthService
builder.Services.AddScoped<IAuthService, AuthService>();

// Đăng ký GuestService
builder.Services.AddScoped<IGuestService, GuestService>();

// Đăng ký Service Module
builder.Services.AddScoped<IServicePackageService, ServicePackageService>();
builder.Services.AddHostedService<SubscriptionExpiryJob>();

// Đăng ký IOnboardingService
builder.Services.AddScoped<IOnboardingService, OnboardingService>();

// Đăng ký ITindbookService
builder.Services.AddScoped<ITindbookService, TindbookService>();

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
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? "ChuoiBiMatMacDinhSieuDaiCuaBan123!"))
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

    var securityScheme = new OpenApiModels.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "Nhập token theo định dạng: Bearer {your_token}",
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
});

builder.Services.AddControllersWithViews();
builder.Services.AddAuthorization(options =>
{
    // Hằng số cho AccountStatus.Active
    var activeStatus = ((int)AccountStatus.Active).ToString();

    // 1. Policy cho Admin (Toàn quyền)
    options.AddPolicy("AdminOnly", policy => 
    {
        policy.RequireClaim(ClaimTypes.Role, ((int)UserRole.SystemAdmin).ToString());
        policy.RequireClaim("AccountStatus", activeStatus);
    });

    // 2. Policy cho tất cả Staff (Admin, Moderator, Marketing, Store)
    options.AddPolicy("StaffOnly", policy =>
    {
        policy.RequireClaim(ClaimTypes.Role, 
            ((int)UserRole.SystemAdmin).ToString(),
            ((int)UserRole.Moderator).ToString(),
            ((int)UserRole.MarketingManager).ToString(),
            ((int)UserRole.StoreManager).ToString());
        policy.RequireClaim("AccountStatus", activeStatus);
    });

    // 3. Policy riêng cho Marketing
    options.AddPolicy("MarketingManagerOnly", policy =>
    {
        policy.RequireClaim(ClaimTypes.Role, 
            ((int)UserRole.SystemAdmin).ToString(),
            ((int)UserRole.MarketingManager).ToString());
        policy.RequireClaim("AccountStatus", activeStatus);
    });

    // 4. Policy riêng cho Moderator
    options.AddPolicy("ModeratorOnly", policy =>
    {
        policy.RequireClaim(ClaimTypes.Role, 
            ((int)UserRole.SystemAdmin).ToString(),
            ((int)UserRole.Moderator).ToString());
        policy.RequireClaim("AccountStatus", activeStatus);
    });

    // 5. Policy riêng cho Store Manager
    options.AddPolicy("StoreManagerOnly", policy =>
    {
        policy.RequireClaim(ClaimTypes.Role, 
            ((int)UserRole.SystemAdmin).ToString(),
            ((int)UserRole.StoreManager).ToString());
        policy.RequireClaim("AccountStatus", activeStatus);
    });
            
    // 6. Policy cho Khách hàng đã định danh
    options.AddPolicy("CustomerOnly", policy =>
    {
        policy.RequireClaim(ClaimTypes.Role, ((int)UserRole.Customer).ToString());
        policy.RequireClaim("AccountStatus", activeStatus);
    });
});
var app = builder.Build();

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
app.MapControllers();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
