using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using BookBlossom.Core.Entities;
using BookBlossom.Core.Interfaces.Services; // Ensure using the correct namespace where IOTPService is defined
using BookBlossom.Infrastructure.Data;

namespace BookBlossom.Infrastructure.Services
{
    public class OTPService : IOTPService
    {
        private readonly ApplicationDbContext _context;

        public OTPService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<string> GenerateOtpAsync(string phoneNumber, string ipAddress)
        {
            var today = DateTime.UtcNow.Date;

            // Kiểm tra cooldown 60 giây
            var lastOtp = await _context.OTPLogs
                .Where(x => x.PhoneNumber == phoneNumber)
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefaultAsync();

            if (lastOtp != null && (DateTime.UtcNow - lastOtp.CreatedAt).TotalSeconds < 60)
            {
                throw new Exception("Vui lòng đợi 60 giây trước khi yêu cầu mã OTP mới.");
            }

            // Kiểm tra số lần đã gửi trong ngày
            var sendCountToday = await _context.OTPLogs
                .Where(x => x.PhoneNumber == phoneNumber && x.CreatedAt >= today)
                .CountAsync();

            if (sendCountToday >= 5)
            {
                throw new Exception("Bạn đã vượt quá giới hạn 5 lần nhận OTP trong ngày!");
            }

            // Kiểm tra giới hạn theo IP (ví dụ: tối đa 20 OTP/ngày/IP)
            var ipCountToday = await _context.OTPLogs
                .Where(x => x.IpAddress == ipAddress && x.CreatedAt >= today)
                .CountAsync();

            if (ipCountToday >= 20)
            {
                throw new Exception("Địa chỉ IP của bạn đã yêu cầu quá nhiều mã OTP trong ngày hôm nay.");
            }

            // Tạo mã OTP 6 số
            var random = new Random();
            var otpCode = random.Next(100000, 999999).ToString();

            // Hash OTP trước khi lưu
            var hashedOtp = BCrypt.Net.BCrypt.HashPassword(otpCode);

            // Tạo bản ghi log
            var otpLog = new OTPLog
            {
                PhoneNumber = phoneNumber,
                OTPCode = hashedOtp,
                CreatedAt = DateTime.UtcNow,
                ExpireAt = DateTime.UtcNow.AddMinutes(5), // Hết hạn sau 5 phút
                IsUsed = false,
                FailedAttempts = 0,
                IsLocked = false,
                IpAddress = ipAddress
            };

            _context.OTPLogs.Add(otpLog);
            await _context.SaveChangesAsync();

            return otpCode;
        }

        public async Task<bool> VerifyOtpAsync(string phoneNumber, string otpCode)
        {
            var latestOtp = await _context.OTPLogs
                .Where(x => x.PhoneNumber == phoneNumber)
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefaultAsync();

            if (latestOtp == null)
            {
                throw new Exception("Không tìm thấy mã OTP cho số điện thoại này.");
            }

            if (latestOtp.IsLocked)
            {
                throw new Exception("Mã OTP này đã bị khóa do nhập sai quá nhiều lần.");
            }

            if (latestOtp.ExpireAt < DateTime.UtcNow)
            {
                throw new Exception("Mã OTP đã hết hạn.");
            }

            if (latestOtp.IsUsed)
            {
                throw new Exception("Mã OTP đã được sử dụng.");
            }

            // Kiểm tra mã OTP
            if (!BCrypt.Net.BCrypt.Verify(otpCode, latestOtp.OTPCode))
            {
                latestOtp.FailedAttempts++;
                if (latestOtp.FailedAttempts >= 5)
                {
                    latestOtp.IsLocked = true;
                }
                await _context.SaveChangesAsync();
                return false;
            }

            // Đánh dấu là đã sử dụng
            latestOtp.IsUsed = true;
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
