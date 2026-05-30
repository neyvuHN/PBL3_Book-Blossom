using Microsoft.AspNetCore.Mvc;

namespace BookBlossom.Web.Controllers
{
    // [Authorize(Roles = "SystemAdmin, Moderator, MarketingManager, StoreManager")]
    public class AdminController : Controller
    {
        public IActionResult Dashboard()
        {
            return View();
        }
        
        public IActionResult Users()
        {
            // Populate mock data matching the UI specifications and requirements
            var model = new ViewModels.Admin.UserManagementViewModel
            {
                Buyers = new List<ViewModels.Admin.AdminUserItemViewModel>
                {
                    new()
                    {
                        Id = "buyer_2",
                        Username = "user_john_d",
                        Email = "joh.d@email.com",
                        Role = "User",
                        Plan = "Basic",
                        InternalScore = 45, // Under 50 will trigger caution warning
                        JoinDate = "Jan 05, 2024",
                        Status = "Active",
                        AvatarUrl = "https://i.pravatar.cc/150?img=4"
                    },
                    new()
                    {
                        Id = "buyer_3",
                        Username = "buyer_alice",
                        Email = "alice@reading.com",
                        Role = "User",
                        Plan = "Free",
                        InternalScore = 94,
                        JoinDate = "Feb 12, 2024",
                        Status = "Active",
                        AvatarUrl = "https://i.pravatar.cc/150?img=5"
                    }
                },
                Staff = new List<ViewModels.Admin.AdminUserItemViewModel>
                {
                    new()
                    {
                        Id = "staff_1",
                        Username = "admin_sarah",
                        Email = "sarah@bookblossom.com",
                        Role = "Admin",
                        Plan = "Pro", // Staff standard
                        InternalScore = 98,
                        JoinDate = "Oct 24, 2023",
                        Status = "Active",
                        AvatarUrl = "https://i.pravatar.cc/150?img=1"
                    },
                    new()
                    {
                        Id = "staff_2",
                        Username = "mod_mike",
                        Email = "mike@bookblossom.com",
                        Role = "Moderator",
                        Plan = "Pro",
                        InternalScore = 92,
                        JoinDate = "Nov 01, 2023",
                        Status = "Active",
                        AvatarUrl = "https://i.pravatar.cc/150?img=2"
                    }
                },
                Banned = new List<ViewModels.Admin.AdminUserItemViewModel>
                {
                    new()
                    {
                        Id = "banned_1",
                        Username = "spammer_bob",
                        Email = "bob@spambot.com",
                        Role = "User",
                        Plan = "Free",
                        InternalScore = 15,
                        JoinDate = "Mar 02, 2024",
                        Status = "Banned",
                        AvatarUrl = "https://i.pravatar.cc/150?img=6"
                    }
                }
            };

            return View(model);
        }

        public IActionResult Orders()
        {
            return View();
        }

        public IActionResult Inventory()
        {
            return View();
        }

        public IActionResult Marketing()
        {
            return View();
        }

        public IActionResult ContentReports()
        {
            return View();
        }
    }
}
