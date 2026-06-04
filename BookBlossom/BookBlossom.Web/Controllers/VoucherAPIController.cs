using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using BookBlossom.Core.Interfaces.Services;
using System.Linq;
using BookBlossom.Core.Enums;
using System;

namespace BookBlossom.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VoucherAPIController : ControllerBase
    {
        private readonly IVoucherService _voucherService;

        public VoucherAPIController(IVoucherService voucherService)
        {
            _voucherService = voucherService;
        }

        [HttpGet("active")]
        public async Task<IActionResult> GetActiveVouchers()
        {
            try
            {
                var vouchers = await _voucherService.GetAllVouchersAsync();
                
                var now = DateTime.UtcNow;
                var activeVouchers = vouchers.Where(v => 
                    v.StatusVoucher == VoucherStatus.Active &&
                    v.StartDate <= now &&
                    v.EndDate >= now &&
                    v.UsedCount < v.TotalLimit
                ).ToList();

                return Ok(new { success = true, data = activeVouchers });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }
    }
}
