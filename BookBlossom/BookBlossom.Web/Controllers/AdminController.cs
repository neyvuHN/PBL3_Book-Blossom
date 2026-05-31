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

        private static List<ViewModels.Admin.InventoryCategoryViewModel>? _categoriesList;
        private static List<ViewModels.Admin.InventoryCategoryViewModel> _categories
        {
            get
            {
                if (_categoriesList == null)
                {
                    _categoriesList = new()
                    {
                        new() { CategoryID = 1, CategoryName = "Literature & Fiction", Description = "Classic and contemporary literature.", Status = "Active", BookCount = 120 },
                        new() { CategoryID = 2, CategoryName = "Business & Economics", Description = "Finance, management, and economics books.", Status = "Active", BookCount = 85 },
                        new() { CategoryID = 3, CategoryName = "Self-Help & Skills", Description = "Personal development and life skills.", Status = "Active", BookCount = 150 },
                        new() { CategoryID = 4, CategoryName = "Science Fiction", Description = "Sci-fi and fantasy novels.", Status = "Inactive", BookCount = 45 }
                    };
                }
                return _categoriesList;
            }
        }

        private static List<ViewModels.Admin.InventoryBookItemViewModel>? _booksList;
        private static List<ViewModels.Admin.InventoryBookItemViewModel> _books
        {
            get
            {
                if (_booksList == null)
                {
                    _booksList = new()
                    {
                        new()
                        {
                            BookID = 9001,
                            CategoryID = 1,
                            CategoryName = "Literature & Fiction",
                            Title = "The Great Gatsby",
                            Publisher = "Scribner",
                            ISBN = "978-0743273565",
                            Description = "The story of the mysteriously wealthy Jay Gatsby and his love for the beautiful Daisy Buchanan.",
                            Price = 150000,
                            Weight = 300.0,
                            UnitsInStock = 45,
                            ReservedQuantity = 2,
                            IsContinued = true,
                            PublishYear = 2004,
                            MainImageUrl = "/images/Book/book1.jpg"
                        },
                        new()
                        {
                            BookID = 9002,
                            CategoryID = 3,
                            CategoryName = "Self-Help & Skills",
                            Title = "Atomic Habits",
                            Publisher = "Avery",
                            ISBN = "978-0735211292",
                            Description = "An easy and proven way to build good habits and break bad ones.",
                            Price = 220000,
                            Weight = 350.0,
                            UnitsInStock = 12,
                            ReservedQuantity = 5,
                            IsContinued = true,
                            PublishYear = 2018,
                            MainImageUrl = "/images/Book/book2.webp"
                        },
                        new()
                        {
                            BookID = 9003,
                            CategoryID = 4,
                            CategoryName = "Science Fiction",
                            Title = "Dune",
                            Publisher = "Ace Books",
                            ISBN = "978-0441172719",
                            Description = "Set in the far future amidst a sprawling feudal interstellar empire.",
                            Price = 180000,
                            Weight = 500.0,
                            UnitsInStock = 0,
                            ReservedQuantity = 0,
                            IsContinued = false,
                            PublishYear = 1965,
                            MainImageUrl = "/images/Book/book1.jpg"
                        }
                    };
                }
                return _booksList;
            }
        }

        public IActionResult Inventory()
        {
            var model = new ViewModels.Admin.InventoryManagementViewModel
            {
                Categories = _categories,
                Books = _books
            };

            return View(model);
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

        [HttpPost]
        public async Task<IActionResult> AddBook(
            [FromForm] ViewModels.Admin.InventoryBookItemViewModel bookInput,
            List<IFormFile> BookImages,
            IFormFile? SampleFile,
            [FromServices] IWebHostEnvironment env)
        {
            // 1. Process and save the Sample File if uploaded
            string? sampleFilePath = null;
            if (SampleFile != null && SampleFile.Length > 0)
            {
                var sampleDir = Path.Combine(env.WebRootPath, "samples");
                if (!Directory.Exists(sampleDir)) Directory.CreateDirectory(sampleDir);

                var uniqueSampleName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(SampleFile.FileName);
                var fullSamplePath = Path.Combine(sampleDir, uniqueSampleName);

                using (var stream = new FileStream(fullSamplePath, FileMode.Create))
                {
                    await SampleFile.CopyToAsync(stream);
                }
                sampleFilePath = $"/samples/{uniqueSampleName}";
            }

            // 2. Save physical images to wwwroot/images/Book
            var uploadDir = Path.Combine(env.WebRootPath, "images", "Book");
            if (!Directory.Exists(uploadDir))
            {
                Directory.CreateDirectory(uploadDir);
            }

            var savedImagePaths = new List<string>();
            if (BookImages != null && BookImages.Any())
            {
                foreach (var file in BookImages)
                {
                    if (file.Length > 0)
                    {
                        var uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(file.FileName);
                        var filePath = Path.Combine(uploadDir, uniqueFileName);

                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await file.CopyToAsync(stream);
                        }
                        savedImagePaths.Add($"/images/Book/{uniqueFileName}");
                    }
                }
            }

            // 3. Add to In-Memory static list
            var newId = _books.Any() ? _books.Max(b => b.BookID) + 1 : 9001;
            var categoryName = _categories.FirstOrDefault(c => c.CategoryID == bookInput.CategoryID)?.CategoryName ?? "Unknown";
            
            var newBook = new ViewModels.Admin.InventoryBookItemViewModel
            {
                BookID = newId,
                CategoryID = bookInput.CategoryID,
                CategoryName = categoryName,
                Title = bookInput.Title,
                Publisher = bookInput.Publisher ?? string.Empty,
                ISBN = bookInput.ISBN ?? string.Empty,
                Description = bookInput.Description ?? string.Empty,
                Price = bookInput.Price,
                SampleFilePath = sampleFilePath,
                Weight = bookInput.Weight,
                UnitsInStock = bookInput.UnitsInStock,
                IsContinued = bookInput.IsContinued,
                ReservedQuantity = 0,
                PublishYear = bookInput.PublishYear,
                MainImageUrl = savedImagePaths.FirstOrDefault() ?? "/images/Book/book1.jpg"
            };

            _books.Add(newBook);

            // Increment category book count
            var cat = _categories.FirstOrDefault(c => c.CategoryID == bookInput.CategoryID);
            if (cat != null)
            {
                cat.BookCount++;
            }

            TempData["SuccessMessage"] = "Book added successfully to in-memory list!";
            return RedirectToAction("Inventory");
        }

        [HttpPost]
        public IActionResult EditBook(long id, [FromForm] ViewModels.Admin.InventoryBookItemViewModel bookInput)
        {
            var book = _books.FirstOrDefault(b => b.BookID == id);
            if (book != null)
            {
                // Decrement book count of old category if it changed
                if (book.CategoryID != bookInput.CategoryID)
                {
                    var oldCat = _categories.FirstOrDefault(c => c.CategoryID == book.CategoryID);
                    if (oldCat != null) oldCat.BookCount = Math.Max(0, oldCat.BookCount - 1);

                    var newCat = _categories.FirstOrDefault(c => c.CategoryID == bookInput.CategoryID);
                    if (newCat != null) newCat.BookCount++;
                }

                book.CategoryID = bookInput.CategoryID;
                book.CategoryName = _categories.FirstOrDefault(c => c.CategoryID == bookInput.CategoryID)?.CategoryName ?? "Unknown";
                book.Title = bookInput.Title;
                book.Publisher = bookInput.Publisher ?? string.Empty;
                book.ISBN = bookInput.ISBN ?? string.Empty;
                book.Description = bookInput.Description ?? string.Empty;
                book.Price = bookInput.Price;
                book.Weight = bookInput.Weight;
                book.UnitsInStock = bookInput.UnitsInStock;
                book.IsContinued = bookInput.IsContinued;
                book.PublishYear = bookInput.PublishYear;

                TempData["SuccessMessage"] = "Book updated successfully!";
            }
            return RedirectToAction("Inventory");
        }

        [HttpGet]
        public IActionResult DeleteBook(long id)
        {
            var book = _books.FirstOrDefault(b => b.BookID == id);
            if (book != null)
            {
                _books.Remove(book);

                // Decrement category book count
                var cat = _categories.FirstOrDefault(c => c.CategoryID == book.CategoryID);
                if (cat != null)
                {
                    cat.BookCount = Math.Max(0, cat.BookCount - 1);
                }

                TempData["SuccessMessage"] = "Book deleted successfully!";
            }
            return RedirectToAction("Inventory");
        }

        [HttpPost]
        public IActionResult AddCategory([FromForm] ViewModels.Admin.InventoryCategoryViewModel catInput)
        {
            var newId = _categories.Any() ? _categories.Max(c => c.CategoryID) + 1 : 1;
            var newCat = new ViewModels.Admin.InventoryCategoryViewModel
            {
                CategoryID = newId,
                CategoryName = catInput.CategoryName,
                Description = catInput.Description ?? string.Empty,
                Status = catInput.Status ?? "Active",
                BookCount = 0
            };
            _categories.Add(newCat);
            TempData["SuccessMessage"] = "Category added successfully!";
            return RedirectToAction("Inventory");
        }

        [HttpPost]
        public IActionResult EditCategory(long id, [FromForm] ViewModels.Admin.InventoryCategoryViewModel catInput)
        {
            var cat = _categories.FirstOrDefault(c => c.CategoryID == id);
            if (cat != null)
            {
                cat.CategoryName = catInput.CategoryName;
                cat.Description = catInput.Description ?? string.Empty;
                cat.Status = catInput.Status ?? "Active";

                // Update category name inside all books under this category
                foreach (var book in _books.Where(b => b.CategoryID == id))
                {
                    book.CategoryName = catInput.CategoryName;
                }

                TempData["SuccessMessage"] = "Category updated successfully!";
            }
            return RedirectToAction("Inventory");
        }

        [HttpGet]
        public IActionResult DeleteCategory(long id)
        {
            var cat = _categories.FirstOrDefault(c => c.CategoryID == id);
            if (cat != null)
            {
                // Check if any books are currently tagged with this category
                var hasBooks = _books.Any(b => b.CategoryID == id);
                if (hasBooks)
                {
                    TempData["ErrorMessage"] = "Cannot delete category because there are books currently linked to it.";
                }
                else
                {
                    _categories.Remove(cat);
                    TempData["SuccessMessage"] = "Category deleted successfully!";
                }
            }
            return RedirectToAction("Inventory");
        }
    }
}
