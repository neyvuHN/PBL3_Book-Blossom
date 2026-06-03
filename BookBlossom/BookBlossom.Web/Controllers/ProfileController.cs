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
                    .ThenInclude(cd => cd.BadgeCustomers)
                        .ThenInclude(bc => bc.Badge)
                .Include(u => u.CustomerService)
                    .ThenInclude(cs => cs.ServicePackage)
                .FirstOrDefaultAsync(u => u.UserID == userId);

            if (user == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            var reputation = user.CustomerDetail?.CustomerReputations?.FirstOrDefault();

            // Count actual thread posts for current month from DB (more reliable than cached counter)
            var now = System.DateTime.UtcNow;
            var actualThreadCount = await _context.ThreadPosts
                .CountAsync(tp => tp.CustomerID == userId
                    && tp.CreatedAt.Year == now.Year
                    && tp.CreatedAt.Month == now.Month);

            // Load ALL badge names from DB
            var allBadgeNames = await _context.Badges
                .OrderBy(b => b.BadgeID)
                .Select(b => b.BadgeName)
                .ToListAsync();

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
                MemberSince = System.DateTime.Now,
                
                MembershipTier = user.CustomerDetail?.MembershipRank?.RankType?.ToString() ?? "Đồng",
                TotalSpending = user.CustomerDetail?.TotalSpending ?? 0,
                NextTierThreshold = 5000000,
                
                ReputationScore = reputation?.ReputationPoint ?? 100,
                MaxReputationScore = 150,
                
                CurrentOrderStreak = user.CustomerDetail?.CurrentOrderStreak ?? 0,
                
                Badges = user.CustomerDetail?.BadgeCustomers?.Select(bc => bc.Badge.BadgeName).ToList() ?? new List<string>(),
                BadgeEarnedDates = user.CustomerDetail?.BadgeCustomers?
                    .GroupBy(bc => bc.Badge.BadgeName)
                    .ToDictionary(
                        g => g.Key,
                        g => g.First().EarnedAt.ToString("MMMM dd, yyyy")
                    ) ?? new Dictionary<string, string>(),
                AllBadgeNames = allBadgeNames,
                
                SubscriptionPackage = user.CustomerService?.ServicePackage?.PackageName ?? "Free",
                CurrentMonthThreadCount = actualThreadCount,
                MaxMonthlyThreadLimit = user.CustomerService?.ServicePackage?.ThreadLimit ?? 3,
                LastThreadResetDate = user.CustomerDetail?.LastThreadResetDate ?? System.DateTime.Now,
                
                DailyUndoCount = (user.CustomerDetail?.LastUndoDate?.Date == System.DateTime.UtcNow.Date)
                    ? (user.CustomerDetail?.DailyUndoCount ?? 0)
                    : 0,
                MaxDailyUndoLimit = user.CustomerService?.ServicePackage?.UndoLimit ?? 2,
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
