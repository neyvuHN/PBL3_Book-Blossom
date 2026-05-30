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
            var model = new ViewModels.Admin.OrderManagementViewModel
            {
                Orders = new List<ViewModels.Admin.AdminOrderItemViewModel>
                {
                    new()
                    {
                        Id = "ORD-8821",
                        BookTitle = "The Secret Life of Sunflowers",
                        Quantity = 1,
                        Isbn = "978-1400065393",
                        ImagePreviewUrl = "/images/Book/book1.jpg",
                        BuyerName = "Sarah Jones",
                        BuyerAvatarUrl = "https://i.pravatar.cc/150?img=47",
                        CustomerNote = "Note: Please pack carefully!",
                        TotalAmount = 250000,
                        FundsStatus = "Held in Escrow",
                        Status = "Pending Confirmation",
                        SubStatus = "",
                        CreatedAt = "2 hours ago",
                        RemainingTimeText = "46h 12m",
                        RemainingHours = 46.2,
                        IsBlindDate = false
                    },
                    new()
                    {
                        Id = "ORD-8822",
                        BookTitle = "Mystery Book",
                        Quantity = 1,
                        Isbn = "Blind Date Book",
                        ImagePreviewUrl = "/images/BlindDateBook/BlindBook.jpg",
                        BuyerName = "Mike Smith",
                        BuyerAvatarUrl = "https://i.pravatar.cc/150?img=53",
                        CustomerNote = "",
                        TotalAmount = 120000,
                        FundsStatus = "Held in Escrow",
                        Status = "To Ship",
                        SubStatus = "Packing",
                        CreatedAt = "4 hours ago",
                        RemainingTimeText = "",
                        RemainingHours = 0,
                        IsBlindDate = true,
                        BlindDateHiddenTitle = "The Silent Patient (hidden from buyer)",
                        BlindDateNote = "This is a Blind Date order. DO NOT write the title on the external packaging!",
                        Genre = "#Romance #Cozy"
                    },
                    new()
                    {
                        Id = "ORD-8823",
                        BookTitle = "Harry Potter",
                        Quantity = 1,
                        Isbn = "978-0439708180",
                        ImagePreviewUrl = "/images/Book/book2.webp",
                        BuyerName = "Emily Chen",
                        BuyerAvatarUrl = "https://i.pravatar.cc/150?img=32",
                        CustomerNote = "",
                        TotalAmount = 85000,
                        FundsStatus = "Held in Escrow",
                        Status = "In Transit",
                        SubStatus = "In Transit",
                        CreatedAt = "1 day ago",
                        RemainingTimeText = "",
                        RemainingHours = 0,
                        IsBlindDate = false
                    }
                },
                ReturnedItems = new List<ViewModels.Admin.AdminReturnedItemViewModel>
                {
                    new()
                    {
                        Id = "RET-101",
                        OrderId = "ORD-8715",
                        BookTitle = "Principles of Chemistry",
                        Quantity = 1,
                        RefundAmount = 480000,
                        ModeratorDecision = "Approve Return & Refund",
                        ReturnReason = "Wrong textbook edition sent by mistake",
                        RestockStatus = "Pending Restock",
                        TransferredDate = "Yesterday"
                    },
                    new()
                    {
                        Id = "RET-102",
                        OrderId = "ORD-8720",
                        BookTitle = "Data Structures & Algorithms",
                        Quantity = 1,
                        RefundAmount = 320000,
                        ModeratorDecision = "Approve Return & Refund",
                        ReturnReason = "Book arrived with severe water damage",
                        RestockStatus = "Restocked",
                        TransferredDate = "2 days ago"
                    }
                },
                Complaints = new List<ViewModels.Admin.AdminEscalatedComplaintViewModel>
                {
                    new()
                    {
                        Id = "CMP-301",
                        OrderId = "ORD-8799",
                        BuyerName = "Laura Watson",
                        ContactEmail = "laura.w@readingmail.com",
                        Type = "Review",
                        Rating = 2,
                        Content = "The packaging was ripped and the book corners were dented! Very upset.",
                        ModeratorNote = "Buyer left a 2-star review citing poor packaging. Escalated to Store Manager to resolve and offer store points credit.",
                        Status = "Pending Support",
                        TransferredDate = "Today, 10:15 AM"
                    },
                    new()
                    {
                        Id = "CMP-302",
                        OrderId = "ORD-8752",
                        BuyerName = "James Carter",
                        ContactEmail = "j.carter@techcorp.com",
                        Type = "Complaint",
                        Rating = 1,
                        Content = "Ordered a signed copy, but received a standard copy instead. I want to swap or complain.",
                        ModeratorNote = "Escalated complaint. Please contact user directly to coordinate replacement shipping.",
                        Status = "Resolved",
                        TransferredDate = "3 days ago"
                    }
                }
            };

            return View(model);
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

        public IActionResult Messages()
        {
            return View();
        }
    }
}
