using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using BookBlossom.Core.Entities;
using BookBlossom.Core.Enums;
using BookBlossom.Core.Interfaces.Services;
using BookBlossom.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BookBlossom.Web.Areas.Management.Controllers
{
    [Area("Management")]
    [Route("api/management/[controller]")]
    [ApiController]
    [Authorize(Policy = "RequireSystemAdmin")]
    public class SystemAdminController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _auditService;

        public SystemAdminController(ApplicationDbContext context, IAuditService auditService)
        {
            _context = context;
            _auditService = auditService;
        }

        // 1. Lấy danh sách Audit Logs với bộ lọc
        [HttpGet("audit-logs")]
        public async Task<IActionResult> GetAuditLogs([FromQuery] ActionType? actionType, [FromQuery] string? tableName)
        {
            var query = _context.AuditLogs.AsQueryable();

            if (actionType.HasValue)
                query = query.Where(l => l.ActionType == actionType.Value);

            if (!string.IsNullOrEmpty(tableName))
                query = query.Where(l => l.TableName == tableName);

            var logs = await query
                .OrderByDescending(l => l.LogID)
                .ToListAsync();

            return Ok(logs);
        }

        // 2. Lấy danh sách toàn bộ cấu hình hệ thống
        [HttpGet("configurations")]
        public async Task<IActionResult> GetConfigurations()
        {
            var configs = await _context.SystemConfigurations.ToListAsync();
            return Ok(configs);
        }

        // 3. Khóa tài khoản người dùng chủ động
        [HttpPut("users/{userId:long}/lock")]
        public async Task<IActionResult> LockUser(long userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound($"Không tìm thấy người dùng với ID {userId}.");

            if (user.AccountStatus == AccountStatus.Banned)
            {
                return BadRequest("Tài khoản này đã bị khóa từ trước.");
            }

            var adminIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(adminIdStr) || !long.TryParse(adminIdStr, out long adminId))
            {
                return Unauthorized(new { message = "Không xác thực được quản trị viên." });
            }

            var ipAddress = GetClientIpAddress();
            var oldStatus = user.AccountStatus.ToString();

            user.AccountStatus = AccountStatus.Banned;
            user.IsActive = false;

            await _context.SaveChangesAsync();

            // Ghi Audit Log cho hành động khóa tài khoản (Ghi rõ tên tài khoản)
            await _auditService.LogActionAsync(
                adminId,
                user.UserID,
                ActionType.LOCK_ACCOUNT,
                "Users",
                $"Trạng thái cũ: {oldStatus}",
                $"Khóa tài khoản '{user.UserName}' (Trạng thái mới: Banned/Locked)",
                ipAddress
            );

            return Ok(new { message = $"Khóa tài khoản {user.UserName} thành công." });
        }

        // 4. Mở khóa tài khoản người dùng chủ động
        [HttpPut("users/{userId:long}/unlock")]
        public async Task<IActionResult> UnlockUser(long userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound($"Không tìm thấy người dùng với ID {userId}.");

            if (user.AccountStatus == AccountStatus.Active)
            {
                return BadRequest("Tài khoản này đang ở trạng thái hoạt động.");
            }

            var adminIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(adminIdStr) || !long.TryParse(adminIdStr, out long adminId))
            {
                return Unauthorized(new { message = "Không xác thực được quản trị viên." });
            }

            var ipAddress = GetClientIpAddress();
            var oldStatus = user.AccountStatus.ToString();

            user.AccountStatus = AccountStatus.Active;
            user.IsActive = true;

            await _context.SaveChangesAsync();

            // Ghi Audit Log cho hành động mở khóa tài khoản (Ghi rõ tên tài khoản)
            await _auditService.LogActionAsync(
                adminId,
                user.UserID,
                ActionType.UNLOCK_ACCOUNT,
                "Users",
                $"Trạng thái cũ: {oldStatus}",
                $"Mở khóa tài khoản '{user.UserName}' (Trạng thái mới: Active/Unlocked)",
                ipAddress
            );

            return Ok(new { message = $"Mở khóa tài khoản {user.UserName} thành công." });
        }

        private string GetClientIpAddress()
        {
            var ipAddress = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (string.IsNullOrEmpty(ipAddress))
            {
                ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            }
            if (string.IsNullOrEmpty(ipAddress) || ipAddress == "::1")
            {
                ipAddress = "127.0.0.1";
            }
            return ipAddress;
        }
    }
}
