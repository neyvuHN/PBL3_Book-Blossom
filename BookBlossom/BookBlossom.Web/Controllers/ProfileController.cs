using BookBlossom.Infrastructure.Data;
using BookBlossom.Core.Entities;
using BookBlossom.Web.ViewModels.Profile;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Linq;

namespace BookBlossom.Web.Controllers
{
    [Authorize(Roles = "Customer,Admin")]
    public class ProfileController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProfileController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("/Profile")]
        public async Task<IActionResult> Index()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr) || !long.TryParse(userIdStr, out long userId))
            {
                return RedirectToAction("Login", "Auth");
            }

            var user = await _context.Users
                .Include(u => u.CustomerDetail)
                    .ThenInclude(cd => cd.CustomerReputations)
                .Include(u => u.CustomerDetail)
                    .ThenInclude(cd => cd.MembershipRank)
                .Include(u => u.CustomerDetail)
                    .ThenInclude(cd => cd.ServicePackage)
                .FirstOrDefaultAsync(u => u.UserID == userId);

            if (user == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            var reputation = user.CustomerDetail?.CustomerReputations?.FirstOrDefault();

            var model = new ProfileViewModel
            {
                Avatar = string.IsNullOrEmpty(user.Avatar) ? "/images/Avatar/default.jpg" : user.Avatar,
                FullName = $"{user.LastName} {user.FirstName}".Trim(),
                Username = user.UserName ?? "Unknown",
                PhoneNumber = user.PhoneNumber ?? "",
                Email = user.Email ?? "",
                Gender = user.Gender ?? "Unknown",
                Birthdate = user.Birthday ?? new System.DateTime(2000, 1, 1),
                Bio = user.Note ?? "Cập nhật tiểu sử của bạn tại đây.",
                MemberSince = System.DateTime.Now, // User doesn't have CreatedAt currently
                
                MembershipTier = user.CustomerDetail?.MembershipRank?.RankType?.ToString() ?? "Đồng",
                TotalSpending = user.CustomerDetail?.TotalSpending ?? 0,
                NextTierThreshold = 5000000, // Hardcode for now, could be calculated based on next rank
                
                ReputationScore = reputation?.ReputationPoint ?? 100,
                MaxReputationScore = 150,
                
                CurrentOrderStreak = user.CustomerDetail?.CurrentOrderStreak ?? 0,
                
                SubscriptionPackage = user.CustomerDetail?.ServicePackage?.PackageName ?? "Free",
                CurrentMonthThreadCount = user.CustomerDetail?.CurrentMonthThreadCount ?? 0,
                MaxMonthlyThreadLimit = user.CustomerDetail?.ServicePackage?.ThreadLimit ?? 3,
                LastThreadResetDate = user.CustomerDetail?.LastThreadResetDate ?? System.DateTime.Now,
                
                DailyUndoCount = user.CustomerDetail?.DailyUndoCount ?? 0,
                MaxDailyUndoLimit = user.CustomerDetail?.ServicePackage?.UndoLimit ?? 2,
                LastUndoDate = user.CustomerDetail?.LastUndoDate ?? System.DateTime.Now,

                DeliveryAddresses = await _context.DeliveryAddresses
                    .Where(da => da.CustomerID == userId)
                    .OrderByDescending(da => da.IsDefault)
                    .ThenByDescending(da => da.AddressID)
                    .Select(da => new DeliveryAddressItem
                    {
                        AddressID = da.AddressID,
                        ReceiverName = da.ReceiverName,
                        PhoneNumber = da.PhoneNumber,
                        DetailAddress = da.DetailAddress,
                        IsDefault = da.IsDefault ?? false
                    }).ToListAsync()
            };

            return View(model);
        }
    }
}
