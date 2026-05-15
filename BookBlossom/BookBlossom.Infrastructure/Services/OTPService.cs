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

        public async Task<string> GenerateOtpAsync(string phoneNumber)
        {
            var today = DateTime.UtcNow.Date;

            // Kiểm tra số lần đã gửi trong ngày
            var sendCountToday = await _context.OTPLogs
                .Where(x => x.PhoneNumber == phoneNumber && x.CreatedAt >= today)
                .CountAsync();

            if (sendCountToday >= 5)
            {
                throw new Exception("Bạn đã vượt quá giới hạn 5 lần nhận OTP trong ngày!");
            }

            // Tạo mã OTP 6 số
            var random = new Random();
            var otpCode = random.Next(100000, 999999).ToString();

            // Tạo bản ghi log
            var otpLog = new OTPLog
            {
                PhoneNumber = phoneNumber,
                OTPCode = otpCode,
                CreatedAt = DateTime.UtcNow,
                ExpireAt = DateTime.UtcNow.AddMinutes(5), // Hết hạn sau 5 phút
                IsUsed = false
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
                return false;
            }

            // Kiểm tra mã, xem đã sử dụng chưa và có còn hạn không
            if (latestOtp.OTPCode == otpCode && 
                latestOtp.IsUsed != true && 
                latestOtp.ExpireAt > DateTime.UtcNow)
            {
                // Đánh dấu là đã sử dụng
                latestOtp.IsUsed = true;
                await _context.SaveChangesAsync();
                
                return true;
            }

            return false;
        }
    }
}
