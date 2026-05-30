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
            // Populate mock data matching the new reputation score rules and specific staff roles
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
                        InternalScore = 45, // Under 60 (Threshold B) -> COD disabled, comments muted
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
                        InternalScore = 94, // Above 80 -> Active/Excellent
                        JoinDate = "Feb 12, 2024",
                        Status = "Active",
                        AvatarUrl = "https://i.pravatar.cc/150?img=5"
                    },
                    new()
                    {
                        Id = "buyer_4",
                        Username = "buyer_restricted_a",
                        Email = "restricted_a@gmail.com",
                        Role = "User",
                        Plan = "Basic",
                        InternalScore = 75, // Under 80 (Threshold A) -> post & comment muted
                        JoinDate = "Mar 10, 2024",
                        Status = "Active",
                        AvatarUrl = "https://i.pravatar.cc/150?img=12"
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
                        Plan = "Pro",
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
                    },
                    new()
                    {
                        Id = "staff_3",
                        Username = "mkt_manager_lee",
                        Email = "lee.mkt@bookblossom.com",
                        Role = "Marketing Manager",
                        Plan = "Pro",
                        InternalScore = 87,
                        JoinDate = "Jan 12, 2024",
                        Status = "Active",
                        AvatarUrl = "https://i.pravatar.cc/150?img=11"
                    },
                    new()
                    {
                        Id = "staff_4",
                        Username = "store_mgr_anna",
                        Email = "anna.store@bookblossom.com",
                        Role = "Store Manager",
                        Plan = "Pro",
                        InternalScore = 91,
                        JoinDate = "Feb 05, 2024",
                        Status = "Active",
                        AvatarUrl = "https://i.pravatar.cc/150?img=10"
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
                        InternalScore = 15, // Under 30 (Threshold C) -> Khai trừ / Banned
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
