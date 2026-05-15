using BookBlossom.Core.Interfaces.Services;
using BookBlossom.Infrastructure.BackgroundJobs;
using BookBlossom.Infrastructure.Data;
using BookBlossom.Infrastructure.Services;
using BookBlossom.Web.Middlewares;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using OpenApiModels = global::Microsoft.OpenApi.Models;

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
builder.Services.AddAuthorization();

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
