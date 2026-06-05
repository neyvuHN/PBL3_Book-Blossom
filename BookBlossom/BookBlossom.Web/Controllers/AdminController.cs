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

        public IActionResult SystemLogs()
        {
            var model = new ViewModels.Admin.SystemLogsViewModel
            {
                Logs = new List<ViewModels.Admin.AuditLogItemViewModel>()
            };
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> GetSystemLogsData()
        {
            var logs = await _context.AuditLogs
                .OrderByDescending(l => l.LogID)
                .Take(1000)
                .ToListAsync();

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
                    CreatedAt = l.CreatedAt?.ToString("dd/MM/yyyy HH:mm:ss")
                })
                .ToList()
            };
            return Json(model);
        }

        public IActionResult Dashboard()
        {
            return View();
        }

        public IActionResult Users()
        {
            var model = new ViewModels.Admin.UserManagementViewModel
            {
                Buyers = new List<ViewModels.Admin.AdminUserItemViewModel>(),
                Staff = new List<ViewModels.Admin.AdminUserItemViewModel>(),
                Banned = new List<ViewModels.Admin.AdminUserItemViewModel>()
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> GetUsersData()
        {
            // Removed Auto database adjustments (distribute Free packages and balance resources) from here.
            // This logic should be moved to a one-time seeding script or user registration flow to avoid performance issues on GET requests.

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

                var baseDate = new DateTime(2023, 1, 15);
                var offsetDays = (int)((u.UserID * 37) % 1000);
                var joinDate = u.RoleID == UserRole.Admin && u.StaffDetail != null 
                    ? u.StaffDetail.HireDate.ToString("MMM dd, yyyy") 
                    : baseDate.AddDays(offsetDays).ToString("MMM dd, yyyy");

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
                    AvatarUrl = u.Avatar ?? "https://i.pravatar.cc/150?img=9",
                    Note = u.Note
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

            return Json(model);
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
        public async Task<IActionResult> UpdateBuyerNote([FromQuery] long userId, [FromQuery] string noteText)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound(new { success = false, message = "User not found" });

            var adminIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(adminIdStr) || !long.TryParse(adminIdStr, out long adminId))
            {
                return Unauthorized(new { success = false, message = "Unauthorized admin" });
            }

            user.Note = noteText;
            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            return Json(new { success = true });
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

        public async Task<IActionResult> Orders()
        {
            // Removed heavy seeding from GET method to optimize load time.
            // await EnsureReturnRequestsSeededAsync();

            var model = new ViewModels.Admin.OrderManagementViewModel
            {
                Orders = new List<ViewModels.Admin.AdminOrderItemViewModel>(),
                ReturnedItems = new List<ViewModels.Admin.AdminReturnedItemViewModel>(),
                Complaints = new List<ViewModels.Admin.AdminEscalatedComplaintViewModel>()
            };

            return View(model);
        }

        private async Task EnsureReturnRequestsSeededAsync()
        {
            if (!await _context.ReturnRequests.AnyAsync())
            {
                var orders = await _context.Orders
                    .Include(o => o.OrderDetails)
                    .Where(o => o.OrderStatus == OrderStatus.Completed || o.OrderStatus == OrderStatus.Delivering || o.OrderStatus == OrderStatus.Shipping || o.OrderStatus == OrderStatus.AwaitingPickup)
                    .Take(6)
                    .ToListAsync();

                if (orders.Count == 0)
                {
                    orders = await _context.Orders
                        .Include(o => o.OrderDetails)
                        .Take(6)
                        .ToListAsync();
                }

                if (orders.Count > 0)
                {
                    var reasons = new[]
                    {
                        "Sách bị rách gáy và móp méo nghiêm trọng trong quá trình vận chuyển",
                        "Giao sai phiên bản sách so với đơn đặt hàng của tôi",
                        "Sách in lỗi, có nhiều trang bị mất chữ hoặc trắng tinh",
                        "Nội dung sách không đúng với mô tả giới thiệu trên trang web",
                        "Chất lượng in ấn kém, mực bị lem luốc không đọc được",
                        "Nhận nhầm sách cũ thay vì sách mới nguyên màng co"
                    };

                    for (int i = 0; i < orders.Count; i++)
                    {
                        var order = orders[i];
                        var detail = order.OrderDetails.FirstOrDefault();
                        if (detail == null) continue;

                        var resolutionType = (ResolutionType)(i % 2); // 0 = RefundOnly, 1 = ReturnAndRefund
                        var returnStatus = (ReturnStatus)(i % 3); // 0 = Pending, 1 = Approved, 2 = Rejected

                        var request = new ReturnRequest
                        {
                            OrderID = order.OrderID,
                            BookID = detail.BookID,
                            ReturnQuantity = Math.Max(1, detail.Quantity),
                            RequestDate = DateTime.UtcNow.AddDays(-i - 1),
                            ReturnReason = reasons[i % reasons.Length],
                            ResolutionType = resolutionType,
                            ReturnStatus = returnStatus,
                            RefundAmount = ((detail.UnitPrice) - ((detail.Discount ?? 0) / detail.Quantity)) * detail.Quantity,
                            UnboxVideoPath = "/uploads/return_videos/sample_unbox_video.mp4"
                        };

                        if (returnStatus == ReturnStatus.Approved)
                        {
                            request.RefundCompletedDate = DateTime.UtcNow.AddDays(-i);
                            if (order.PaymentMethod != PaymentMethod.COD)
                            {
                                request.GatewayTransactionID = "REFUND_" + Guid.NewGuid().ToString().Replace("-", "").Substring(0, 12).ToUpper();
                            }
                        }
                        else if (returnStatus == ReturnStatus.Rejected)
                        {
                            request.RejectReason = "Video mở hộp không rõ nét, hoặc không phát hiện lỗi như mô tả.";
                        }

                        _context.ReturnRequests.Add(request);
                    }

                    await _context.SaveChangesAsync();
                }
            }
        }

        [HttpPost]
        public async Task<IActionResult> TransferReturn([FromBody] ViewModels.Admin.AdminReturnedItemViewModel newReturn)
        {
            if (newReturn != null)
            {
                long orderId = 0;
                string cleanOrderId = newReturn.OrderId.Replace("ORD-", "").Trim();
                if (!long.TryParse(cleanOrderId, out orderId))
                {
                    var fallbackOrder = await _context.Orders.FirstOrDefaultAsync();
                    if (fallbackOrder != null) orderId = fallbackOrder.OrderID;
                }

                var order = await _context.Orders
                    .Include(o => o.OrderDetails)
                    .FirstOrDefaultAsync(o => o.OrderID == orderId);

                if (order != null)
                {
                    var detail = order.OrderDetails.FirstOrDefault();
                    long bookId = detail?.BookID ?? 0;

                    if (bookId == 0)
                    {
                        var fallbackBook = await _context.RealBooks.FirstOrDefaultAsync();
                        if (fallbackBook != null) bookId = fallbackBook.BookID;
                    }

                    var returnRequest = new ReturnRequest
                    {
                        OrderID = orderId,
                        BookID = bookId,
                        ReturnQuantity = detail?.Quantity ?? 1,
                        RequestDate = DateTime.UtcNow,
                        ReturnReason = string.IsNullOrEmpty(newReturn.ReturnReason) ? "Yêu cầu trả hàng chuyển từ hệ thống quản trị" : newReturn.ReturnReason,
                        ResolutionType = ResolutionType.ReturnAndRefund,
                        ReturnStatus = ReturnStatus.Pending,
                        RefundAmount = newReturn.RefundAmount > 0 ? newReturn.RefundAmount : ((detail?.UnitPrice ?? 0) - ((detail?.Discount ?? 0) / (detail?.Quantity ?? 1))) * (detail?.Quantity ?? 1),
                        UnboxVideoPath = "/uploads/return_videos/sample_unbox_video.mp4"
                    };

                    _context.ReturnRequests.Add(returnRequest);
                    await _context.SaveChangesAsync();

                    return Json(new { success = true });
                }
            }
            return Json(new { success = false });
        }

        [HttpPost]
        public async Task<IActionResult> TransferComplaint([FromBody] ViewModels.Admin.AdminEscalatedComplaintViewModel newComplaint)
        {
            if (newComplaint != null)
            {
                var review = await _context.Reviews
                    .FirstOrDefaultAsync(r => r.Content == newComplaint.Content);

                long customerId = 0;
                long bookId = 0;

                if (review != null)
                {
                    customerId = review.CustomerID;
                    bookId = review.BookID ?? 0;
                }
                else
                {
                    var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == newComplaint.BuyerName || (u.LastName + " " + u.FirstName).Contains(newComplaint.BuyerName));
                    if (user != null)
                    {
                        customerId = user.UserID;
                    }
                }

                Order? order = null;
                if (customerId > 0 && bookId > 0)
                {
                    order = await _context.Orders
                        .Include(o => o.OrderDetails)
                        .FirstOrDefaultAsync(o => o.CustomerID == customerId && o.OrderDetails.Any(od => od.BookID == bookId));
                }

                if (order == null && customerId > 0)
                {
                    order = await _context.Orders
                        .Include(o => o.OrderDetails)
                        .FirstOrDefaultAsync(o => o.CustomerID == customerId);
                }

                if (order == null)
                {
                    order = await _context.Orders
                        .Include(o => o.OrderDetails)
                        .OrderByDescending(o => o.OrderDate)
                        .FirstOrDefaultAsync();
                }

                if (order != null)
                {
                    var detail = order.OrderDetails.FirstOrDefault();
                    if (bookId == 0)
                    {
                        bookId = detail?.BookID ?? 0;
                    }

                    if (bookId == 0)
                    {
                        var fallbackBook = await _context.RealBooks.FirstOrDefaultAsync();
                        if (fallbackBook != null) bookId = fallbackBook.BookID;
                    }

                    var returnRequest = new ReturnRequest
                    {
                        OrderID = order.OrderID,
                        BookID = bookId,
                        ReturnQuantity = detail?.Quantity ?? 1,
                        RequestDate = DateTime.UtcNow,
                        ReturnReason = string.IsNullOrEmpty(newComplaint.Content) ? "Khiếu nại chuyển từ phản hồi của khách hàng" : newComplaint.Content,
                        ResolutionType = ResolutionType.RefundOnly,
                        ReturnStatus = ReturnStatus.Pending,
                        RefundAmount = ((detail?.UnitPrice ?? 0) - ((detail?.Discount ?? 0) / (detail?.Quantity ?? 1))) * (detail?.Quantity ?? 1),
                        UnboxVideoPath = "/uploads/return_videos/sample_unbox_video.mp4"
                    };

                    _context.ReturnRequests.Add(returnRequest);
                    await _context.SaveChangesAsync();

                    return Json(new { success = true });
                }
            }
            return Json(new { success = false });
        }

        public async Task<IActionResult> Inventory()
        {
            var dbCategoriesList = await _context.Categories
                .Include(c => c.RealBooks)
                .ToListAsync();

            var dbCategories = dbCategoriesList
                .Select(c => new ViewModels.Admin.InventoryCategoryViewModel
                {
                    CategoryID = c.CategoryID,
                    CategoryName = c.CategoryName,
                    Description = c.Description ?? string.Empty,
                    Status = c.Status == CategoryStatus.Active ? "Active" : (c.Status == CategoryStatus.Archived ? "Archived" : "Inactive"),
                    BookCount = c.RealBooks.Count
                })
                .ToList();

            var model = new ViewModels.Admin.InventoryManagementViewModel
            {
                Categories = dbCategories,
                Books = new List<ViewModels.Admin.InventoryBookItemViewModel>()
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

            var newBook = new RealBook
            {
                CategoryID = bookInput.CategoryID,
                Title = bookInput.Title,
                Publisher = bookInput.Publisher ?? string.Empty,
                ISBN = bookInput.ISBN ?? string.Empty,
                Description = bookInput.Description,
                Price = bookInput.Price,
                SampleFilePath = sampleFilePath,
                Weight = (decimal)bookInput.Weight,
                UnitsInStock = bookInput.UnitsInStock,
                ReservedQuantity = 0,
                IsContinued = true,
                PublishYear = bookInput.PublishYear
            };

            _context.RealBooks.Add(newBook);
            await _context.SaveChangesAsync();

            // 2. Save cover image to wwwroot/images/Book/cover_{BookID}.jpg if uploaded
            if (BookImages != null && BookImages.Any())
            {
                var file = BookImages.FirstOrDefault();
                if (file != null && file.Length > 0)
                {
                    var uploadDir = Path.Combine(env.WebRootPath, "images", "Book");
                    if (!Directory.Exists(uploadDir)) Directory.CreateDirectory(uploadDir);

                    var filePath = Path.Combine(uploadDir, $"cover_{newBook.BookID}.jpg");
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }
                }
            }

            TempData["SuccessMessage"] = "Book added successfully!";
            return RedirectToAction("Inventory");
        }

        [HttpPost]
        public async Task<IActionResult> EditBook(
            long id, 
            [FromForm] ViewModels.Admin.InventoryBookItemViewModel bookInput,
            List<IFormFile> BookImages,
            [FromServices] IWebHostEnvironment env)
        {
            var book = await _context.RealBooks.FindAsync(id);
            if (book != null)
            {
                if (bookInput.UnitsInStock < book.ReservedQuantity)
                {
                    TempData["ErrorMessage"] = $"Số lượng tồn kho mới ({bookInput.UnitsInStock}) không thể nhỏ hơn số lượng đang giữ chỗ (Đang giữ chỗ: {book.ReservedQuantity}).";
                    return RedirectToAction("Inventory");
                }

                book.CategoryID = bookInput.CategoryID;
                book.Title = bookInput.Title;
                book.Publisher = bookInput.Publisher ?? string.Empty;
                book.ISBN = bookInput.ISBN ?? string.Empty;
                book.Description = bookInput.Description;
                book.Price = bookInput.Price;
                book.Weight = (decimal)bookInput.Weight;
                book.UnitsInStock = bookInput.UnitsInStock;
                book.IsContinued = bookInput.IsContinued;
                book.PublishYear = bookInput.PublishYear;

                _context.RealBooks.Update(book);
                await _context.SaveChangesAsync();

                // Save cover image to wwwroot/images/Book/cover_{BookID}.jpg if uploaded
                if (BookImages != null && BookImages.Any())
                {
                    var file = BookImages.FirstOrDefault();
                    if (file != null && file.Length > 0)
                    {
                        var uploadDir = Path.Combine(env.WebRootPath, "images", "Book");
                        if (!Directory.Exists(uploadDir)) Directory.CreateDirectory(uploadDir);

                        var filePath = Path.Combine(uploadDir, $"cover_{book.BookID}.jpg");
                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await file.CopyToAsync(stream);
                        }
                    }
                }

                TempData["SuccessMessage"] = "Book updated successfully!";
            }
            return RedirectToAction("Inventory");
        }

        [HttpGet]
        public async Task<IActionResult> DeleteBook(long id)
        {
            var book = await _context.RealBooks.FindAsync(id);
            if (book != null)
            {
                // Soft delete (discontinue)
                book.IsContinued = false;
                _context.RealBooks.Update(book);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Book discontinued successfully!";
            }
            return RedirectToAction("Inventory");
        }

        [HttpPost]
        public async Task<IActionResult> AddCategory([FromForm] ViewModels.Admin.InventoryCategoryViewModel catInput)
        {
            var status = catInput.Status == "Active" ? CategoryStatus.Active : (catInput.Status == "Archived" ? CategoryStatus.Archived : CategoryStatus.Inactive);
            var newCat = new Category
            {
                CategoryName = catInput.CategoryName,
                Description = catInput.Description,
                Status = status
            };
            _context.Categories.Add(newCat);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Category added successfully!";
            return RedirectToAction("Inventory");
        }

        [HttpPost]
        public async Task<IActionResult> EditCategory(long id, [FromForm] ViewModels.Admin.InventoryCategoryViewModel catInput)
        {
            var cat = await _context.Categories.FindAsync(id);
            if (cat != null)
            {
                cat.CategoryName = catInput.CategoryName;
                cat.Description = catInput.Description;
                cat.Status = catInput.Status == "Active" ? CategoryStatus.Active : (catInput.Status == "Archived" ? CategoryStatus.Archived : CategoryStatus.Inactive);

                _context.Categories.Update(cat);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Category updated successfully!";
            }
            return RedirectToAction("Inventory");
        }

        [HttpGet]
        public async Task<IActionResult> DeleteCategory(long id)
        {
            var cat = await _context.Categories.Include(c => c.RealBooks).FirstOrDefaultAsync(c => c.CategoryID == id);
            if (cat != null)
            {
                if (cat.RealBooks.Any(b => b.IsContinued))
                {
                    TempData["ErrorMessage"] = "Cannot delete category because there are active books currently linked to it.";
                }
                else
                {
                    cat.Status = CategoryStatus.Inactive;
                    _context.Categories.Update(cat);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Category set to Inactive successfully!";
                }
            }
            return RedirectToAction("Inventory");
        }
    }
}
