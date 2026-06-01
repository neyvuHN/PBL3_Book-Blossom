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

namespace BookBlossom.Web.Controllers
{
    [Authorize(Policy = "AdminOnly")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IReputationService _reputationService;
        private readonly IAuditService _auditService;

        public AdminController(
            ApplicationDbContext context,
            IReputationService reputationService,
            IAuditService auditService)
        {
            _context = context;
            _reputationService = reputationService;
            _auditService = auditService;
        }

        public async Task<IActionResult> SystemLogs()
        {
            var logs = await _context.AuditLogs.ToListAsync();

            var adminIds = logs.Select(l => l.SystemAdminID).Distinct().ToList();
            var userIds = logs.Where(l => l.UserID.HasValue).Select(l => l.UserID.Value).Distinct().ToList();
            var allUserIds = adminIds.Concat(userIds).Distinct().ToList();

            var usernames = await _context.Users
                .Where(u => allUserIds.Contains(u.UserID))
                .ToDictionaryAsync(u => u.UserID, u => u.UserName);

            var model = new ViewModels.Admin.SystemLogsViewModel
            {
                Logs = logs.Select(l => new ViewModels.Admin.AuditLogItemViewModel
                {
                    LogID = l.LogID,
                    SystemAdminID = l.SystemAdminID,
                    AdminUsername = usernames.ContainsKey(l.SystemAdminID) ? usernames[l.SystemAdminID] : "Unknown Admin",
                    UserID = l.UserID,
                    TargetUsername = l.UserID.HasValue && usernames.ContainsKey(l.UserID.Value) ? usernames[l.UserID.Value] : null,
                    ActionType = (byte)l.ActionType,
                    ActionTypeName = l.ActionType.ToString(),
                    TableName = l.TableName,
                    OldData = l.OldData,
                    NewData = l.NewData,
                    IPAddress = l.IPAddress,
                    CreatedAt = l.CreatedAt?.ToString("yyyy-MM-dd HH:mm:ss")
                })
                .OrderByDescending(x => x.LogID)
                .ToList()
            };
            return View(model);
        }

        public IActionResult Dashboard()
        {
            return View();
        }

        public async Task<IActionResult> Users()
        {
            var allUsers = await _context.Users
                .Include(u => u.CustomerDetail)
                .Include(u => u.StaffDetail)
                .Include(u => u.CustomerService)
                    .ThenInclude(cs => cs.ServicePackage)
                .ToListAsync();

            var reputations = await _context.CustomerReputations.ToDictionaryAsync(r => r.CustomerID, r => r.ReputationPoint ?? 100);

            var buyersList = new List<ViewModels.Admin.AdminUserItemViewModel>();
            var staffList = new List<ViewModels.Admin.AdminUserItemViewModel>();
            var bannedList = new List<ViewModels.Admin.AdminUserItemViewModel>();

            foreach (var u in allUsers)
            {
                if (u.RoleID == UserRole.Guest) continue;

                var score = u.RoleID == UserRole.Admin 
                    ? (int)(u.StaffDetail?.KPIScore ?? 100) 
                    : (reputations.ContainsKey(u.UserID) ? reputations[u.UserID] : 100);

                var joinDate = u.RoleID == UserRole.Admin && u.StaffDetail != null 
                    ? u.StaffDetail.HireDate.ToString("MMM dd, yyyy") 
                    : "Jan 05, 2024";

                var plan = u.CustomerService?.ServicePackage?.PackageName ?? "Free";

                var item = new ViewModels.Admin.AdminUserItemViewModel
                {
                    Id = u.UserID.ToString(),
                    Username = u.UserName,
                    Email = u.Email,
                    Role = u.RoleID.ToString(),
                    Plan = plan,
                    InternalScore = score,
                    JoinDate = joinDate,
                    Status = u.AccountStatus.ToString(),
                    AvatarUrl = u.Avatar ?? "https://i.pravatar.cc/150?img=9"
                };

                if (u.AccountStatus == AccountStatus.Banned || u.IsActive == false)
                {
                    item.Status = "Banned";
                    bannedList.Add(item);
                }
                else if (u.RoleID == UserRole.Admin)
                {
                    staffList.Add(item);
                }
                else if (u.RoleID == UserRole.Customer)
                {
                    buyersList.Add(item);
                }
            }

            var model = new ViewModels.Admin.UserManagementViewModel
            {
                Buyers = buyersList,
                Staff = staffList,
                Banned = bannedList
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> ToggleLock([FromQuery] long userId, [FromQuery] bool isBanned)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound(new { success = false, message = "User not found" });

            var adminIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(adminIdStr) || !long.TryParse(adminIdStr, out long adminId))
            {
                return Unauthorized(new { success = false, message = "Unauthorized admin" });
            }

            var oldStatus = user.AccountStatus.ToString();
            user.AccountStatus = isBanned ? AccountStatus.Banned : AccountStatus.Active;
            user.IsActive = !isBanned;

            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            var action = isBanned ? ActionType.LOCK_ACCOUNT : ActionType.UNLOCK_ACCOUNT;
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";

            await _auditService.LogActionAsync(
                adminId,
                user.UserID,
                action,
                "Users",
                JsonSerializer.Serialize(new { Status = oldStatus }),
                JsonSerializer.Serialize(new { Status = user.AccountStatus.ToString() }),
                ipAddress
            );

            return Json(new { success = true, status = user.AccountStatus.ToString() });
        }

        [HttpPost]
        public async Task<IActionResult> UpdateRole([FromQuery] long userId, [FromQuery] string newRole, [FromQuery] string adminPassword)
        {
            var adminIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(adminIdStr) || !long.TryParse(adminIdStr, out long adminId))
            {
                return Unauthorized(new { success = false, message = "Unauthorized admin" });
            }

            var adminUser = await _context.Users.FindAsync(adminId);
            if (adminUser == null || !BCrypt.Net.BCrypt.Verify(adminPassword, adminUser.Password))
            {
                return BadRequest(new { success = false, message = "Mật khẩu xác thực không đúng!" });
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound(new { success = false, message = "User not found" });

            if (newRole != "Admin" && newRole != "User" && newRole != "Customer")
            {
                return BadRequest(new { success = false, message = "Vai trò không hợp lệ" });
            }

            var oldRole = user.RoleID.ToString();
            var targetRole = (newRole == "Admin") ? UserRole.Admin : UserRole.Customer;

            user.RoleID = targetRole;

            if (targetRole == UserRole.Admin)
            {
                var staff = await _context.StaffDetails.FindAsync(user.UserID);
                if (staff == null)
                {
                    staff = new StaffDetail
                    {
                        StaffID = user.UserID,
                        Address = "N/A",
                        IsOnboardingCompleted = false,
                        Department = Department.SystemAdmin,
                        Position = StaffPosition.Leader,
                        HireDate = DateTime.Today,
                        ContractType = ContractType.FullTime,
                        Salary = 15000000
                    };
                    _context.StaffDetails.Add(staff);
                }
            }
            else
            {
                var customer = await _context.CustomerDetails.FindAsync(user.UserID);
                if (customer == null)
                {
                    customer = new CustomerDetail
                    {
                        CustomerID = user.UserID,
                        IsOnboardingCompleted = true,
                        TotalSpending = 0,
                        DailyUndoCount = 0,
                        CurrentMonthThreadCount = 0,
                        CurrentOrderStreak = 0
                    };
                    _context.CustomerDetails.Add(customer);
                }
            }

            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
            await _auditService.LogActionAsync(
                adminId,
                user.UserID,
                ActionType.LOCK_ACCOUNT,
                "Users",
                JsonSerializer.Serialize(new { Role = oldRole }),
                JsonSerializer.Serialize(new { Role = user.RoleID.ToString() }),
                ipAddress
            );

            return Json(new { success = true, role = (targetRole == UserRole.Admin) ? "Admin" : "User" });
        }

        [HttpPost]
        public async Task<IActionResult> AddAdmin([FromBody] AddAdminDTO dto)
        {
            if (dto == null) return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ" });

            if (await _context.Users.AnyAsync(u => u.UserName == dto.Username))
            {
                return BadRequest(new { success = false, message = "Username đã tồn tại!" });
            }

            var adminIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(adminIdStr) || !long.TryParse(adminIdStr, out long adminId))
            {
                return Unauthorized(new { success = false, message = "Unauthorized admin" });
            }

            var user = new User
            {
                UserName = dto.Username,
                Password = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                Email = dto.Email,
                PhoneNumber = dto.PhoneNumber,
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                Gender = dto.Gender,
                Avatar = dto.AvatarUrl,
                Birthday = DateTime.TryParse(dto.Birthday, out var bday) ? bday : (DateTime?)null,
                RoleID = UserRole.Admin,
                AccountStatus = AccountStatus.Active,
                IsActive = true
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var contract = Enum.TryParse<ContractType>(dto.ContractType.Replace("-", ""), true, out var ct) ? ct : ContractType.FullTime;

            var staff = new StaffDetail
            {
                StaffID = user.UserID,
                Address = "N/A",
                IsOnboardingCompleted = false,
                Department = Department.SystemAdmin,
                Position = StaffPosition.Leader,
                HireDate = DateTime.Today,
                ContractType = contract,
                Salary = dto.Salary,
                BankAccount = dto.BankAccount,
                Experience = dto.Qualifications,
                KPIScore = 100
            };

            _context.StaffDetails.Add(staff);
            await _context.SaveChangesAsync();

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
            await _auditService.LogActionAsync(
                adminId,
                user.UserID,
                ActionType.LOCK_ACCOUNT,
                "Users",
                null,
                JsonSerializer.Serialize(new { Action = "AddAdmin", Username = user.UserName }),
                ipAddress
            );

            var item = new ViewModels.Admin.AdminUserItemViewModel
            {
                Id = user.UserID.ToString(),
                Username = user.UserName,
                Email = user.Email,
                Role = "Admin",
                Plan = "Pro",
                InternalScore = 100,
                JoinDate = staff.HireDate.ToString("MMM dd, yyyy"),
                Status = "Active",
                AvatarUrl = user.Avatar ?? "https://i.pravatar.cc/150?img=1"
            };

            return Json(new { success = true, user = item });
        }

        [HttpPost]
        public async Task<IActionResult> EditAdmin(long userId, [FromBody] AddAdminDTO dto)
        {
            if (dto == null) return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ" });

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound(new { success = false, message = "Staff member not found" });

            var adminIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(adminIdStr) || !long.TryParse(adminIdStr, out long adminId))
            {
                return Unauthorized(new { success = false, message = "Unauthorized admin" });
            }

            var oldData = JsonSerializer.Serialize(new { user.UserName, user.Email, user.PhoneNumber, user.FirstName, user.LastName });

            user.UserName = dto.Username;
            user.Email = dto.Email;
            user.PhoneNumber = dto.PhoneNumber;
            user.FirstName = dto.FirstName;
            user.LastName = dto.LastName;
            user.Gender = dto.Gender;
            user.Avatar = dto.AvatarUrl;
            user.Birthday = DateTime.TryParse(dto.Birthday, out var bday) ? bday : (DateTime?)null;

            if (!string.IsNullOrEmpty(dto.Password) && dto.Password != "123456")
            {
                user.Password = BCrypt.Net.BCrypt.HashPassword(dto.Password);
            }

            var staff = await _context.StaffDetails.FindAsync(userId);
            if (staff == null)
            {
                staff = new StaffDetail
                {
                    StaffID = user.UserID,
                    Address = "N/A",
                    IsOnboardingCompleted = false,
                    Department = Department.SystemAdmin,
                    Position = StaffPosition.Leader,
                    HireDate = DateTime.Today,
                    KPIScore = 100
                };
                _context.StaffDetails.Add(staff);
            }

            staff.ContractType = Enum.TryParse<ContractType>(dto.ContractType.Replace("-", ""), true, out var ct) ? ct : ContractType.FullTime;
            staff.Salary = dto.Salary;
            staff.BankAccount = dto.BankAccount;
            staff.Experience = dto.Qualifications;

            _context.Users.Update(user);
            _context.StaffDetails.Update(staff);
            await _context.SaveChangesAsync();

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
            await _auditService.LogActionAsync(
                adminId,
                user.UserID,
                ActionType.LOCK_ACCOUNT,
                "Users",
                oldData,
                JsonSerializer.Serialize(new { user.UserName, user.Email, user.PhoneNumber, user.FirstName, user.LastName }),
                ipAddress
            );

            var item = new ViewModels.Admin.AdminUserItemViewModel
            {
                Id = user.UserID.ToString(),
                Username = user.UserName,
                Email = user.Email,
                Role = "Admin",
                Plan = "Pro",
                InternalScore = (int)staff.KPIScore,
                JoinDate = staff.HireDate.ToString("MMM dd, yyyy"),
                Status = user.AccountStatus.ToString(),
                AvatarUrl = user.Avatar ?? "https://i.pravatar.cc/150?img=1"
            };

            return Json(new { success = true, user = item });
        }

        public class AddAdminDTO
        {
            public string Username { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
            public string LastName { get; set; } = string.Empty;
            public string FirstName { get; set; } = string.Empty;
            public string PhoneNumber { get; set; } = string.Empty;
            public string Gender { get; set; } = "Male";
            public string Birthday { get; set; } = string.Empty;
            public string ContractType { get; set; } = "FullTime";
            public decimal Salary { get; set; }
            public string BankAccount { get; set; } = string.Empty;
            public string AvatarUrl { get; set; } = string.Empty;
            public string Qualifications { get; set; } = string.Empty;
        }

        public IActionResult Orders()
        {
            var model = new ViewModels.Admin.OrderManagementViewModel
            {
                Orders = new List<ViewModels.Admin.AdminOrderItemViewModel>(),
                ReturnedItems = new List<ViewModels.Admin.AdminReturnedItemViewModel>(),
                Complaints = new List<ViewModels.Admin.AdminEscalatedComplaintViewModel>()
            };

            return View(model);
        }

        private static List<ViewModels.Admin.AdminReturnedItemViewModel>? _returnedItemsList;
        private static List<ViewModels.Admin.AdminReturnedItemViewModel> _returnedItems
        {
            get
            {
                if (_returnedItemsList == null)
                {
                    _returnedItemsList = new List<ViewModels.Admin.AdminReturnedItemViewModel>
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
                    };
                }
                return _returnedItemsList;
            }
        }

        [HttpPost]
        public IActionResult TransferReturn([FromBody] ViewModels.Admin.AdminReturnedItemViewModel newReturn)
        {
            if (newReturn != null)
            {
                newReturn.Id = "RET-" + new Random().Next(103, 999);
                newReturn.ModeratorDecision = "Approve Return & Refund";
                newReturn.RestockStatus = "Pending Restock";
                newReturn.TransferredDate = "Just now";
                
                _returnedItems.Insert(0, newReturn); // Add to top
                return Json(new { success = true });
            }
            return Json(new { success = false });
        }

        private static List<ViewModels.Admin.AdminEscalatedComplaintViewModel>? _complaintsList;
        private static List<ViewModels.Admin.AdminEscalatedComplaintViewModel> _complaints
        {
            get
            {
                if (_complaintsList == null)
                {
                    _complaintsList = new List<ViewModels.Admin.AdminEscalatedComplaintViewModel>
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
                    };
                }
                return _complaintsList;
            }
        }

        [HttpPost]
        public IActionResult TransferComplaint([FromBody] ViewModels.Admin.AdminEscalatedComplaintViewModel newComplaint)
        {
            if (newComplaint != null)
            {
                newComplaint.Id = "CMP-" + new Random().Next(400, 999);
                newComplaint.OrderId = "ORD-XXXX"; // Dummy
                newComplaint.ContactEmail = "N/A";
                newComplaint.Status = "Pending Support";
                newComplaint.TransferredDate = "Just now";
                
                _complaints.Insert(0, newComplaint); // Add to top
                return Json(new { success = true });
            }
            return Json(new { success = false });
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
                            Authors = "F. Scott Fitzgerald",
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
                            Authors = "James Clear",
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
                            Authors = "Frank Herbert",
                            MainImageUrl = "/images/Book/book1.jpg"
                        },
                        new()
                        {
                            BookID = 9004,
                            CategoryID = 1,
                            CategoryName = "Literature & Fiction",
                            Title = "The Secret Garden",
                            Publisher = "Heinemann",
                            ISBN = "978-0140366668",
                            Description = "A story of an orphaned girl who discovers a hidden garden.",
                            Price = 120000,
                            Weight = 250.0,
                            UnitsInStock = 20,
                            ReservedQuantity = 1,
                            IsContinued = true,
                            PublishYear = 1911,
                            Authors = "Frances Hodgson Burnett",
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

        public IActionResult Vouchers()
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
                Authors = bookInput.Authors ?? string.Empty,
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
                var oldBookData = JsonSerializer.Serialize(book);
                
                book.PublishYear = bookInput.PublishYear;
                book.Authors = bookInput.Authors ?? string.Empty;

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
