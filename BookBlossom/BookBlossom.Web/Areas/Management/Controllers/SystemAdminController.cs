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

        // 3. Cập nhật giá trị cấu hình hệ thống (đồng thời ghi Audit Log)
        [HttpPut("configurations/{id:long}")]
        public async Task<IActionResult> UpdateConfiguration(long id, [FromBody] UpdateConfigDTO dto)
        {
            if (dto == null) return BadRequest("Dữ liệu cấu hình trống.");

            var config = await _context.SystemConfigurations.FindAsync(id);
            if (config == null) return NotFound($"Không tìm thấy cấu hình với ID {id}.");

            var adminIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(adminIdStr) || !long.TryParse(adminIdStr, out long adminId))
            {
                return Unauthorized(new { message = "Không xác thực được quản trị viên." });
            }

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
            var oldVal = config.ConfigValue;

            config.ConfigValue = dto.ConfigValue;
            config.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Ghi Audit Log cho hành động cập nhật cấu hình
            await _auditService.LogActionAsync(
                adminId,
                null,
                ActionType.UPDATE,
                "SystemConfiguration",
                $"ConfigName: {config.ConfigName}, Giá trị cũ: {oldVal}",
                $"Giá trị mới: {dto.ConfigValue}",
                ipAddress
            );

            return Ok(config);
        }
    }

    public class UpdateConfigDTO
    {
        public string ConfigValue { get; set; } = string.Empty;
    }
}
