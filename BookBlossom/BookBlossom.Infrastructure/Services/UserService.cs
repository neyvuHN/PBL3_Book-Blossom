using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BookBlossom.Core.DTOs;
using BookBlossom.Core.Entities;
using BookBlossom.Core.Interfaces.Services;
using BookBlossom.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;

namespace BookBlossom.Infrastructure.Services
{
    public class UserService : IUserService
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public UserService(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        public async Task<bool> UpdateProfileAsync(long userId, UpdateProfileDTO dto)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return false;

            if (dto.FirstName != null) user.FirstName = dto.FirstName;
            if (dto.LastName != null) user.LastName = dto.LastName;
            if (dto.Gender != null) user.Gender = dto.Gender;
            
            if (!string.IsNullOrWhiteSpace(dto.UserName) && dto.UserName != user.UserName)
            {
                if (await _context.Users.AnyAsync(u => u.UserName == dto.UserName))
                {
                    throw new Exception("Tên đăng nhập đã tồn tại trong hệ thống.");
                }
                user.UserName = dto.UserName;
            }
            if (dto.Birthdate != null) user.Birthday = dto.Birthdate;
            if (dto.Bio != null) user.Note = dto.Bio;

            if (dto.AvatarImage != null && dto.AvatarImage.Length > 0)
            {
                var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "Avatar");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(dto.AvatarImage.FileName);
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await dto.AvatarImage.CopyToAsync(fileStream);
                }

                user.Avatar = "/images/Avatar/" + uniqueFileName;
            }

            _context.Users.Update(user);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ChangePasswordAsync(long userId, ChangePasswordRequestDTO dto)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return false;

            if (!BCrypt.Net.BCrypt.Verify(dto.OldPassword, user.Password))
            {
                throw new Exception("Mật khẩu cũ không chính xác.");
            }

            user.Password = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
            return true;
        }

        public Task<IEnumerable<AddressDTO>> GetAddressesAsync(long userId)
        {
            throw new NotImplementedException();
        }

        public Task<AddressDTO> AddAddressAsync(long userId, AddressDTO dto)
        {
            throw new NotImplementedException();
        }

        public Task<bool> UpdateAddressAsync(long userId, long addressId, AddressDTO dto)
        {
            throw new NotImplementedException();
        }

        public Task<bool> DeleteAddressAsync(long userId, long addressId)
        {
            throw new NotImplementedException();
        }

        public Task<bool> SetDefaultAddressAsync(long userId, long addressId)
        {
            throw new NotImplementedException();
        }
    }
}
