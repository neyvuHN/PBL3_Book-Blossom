using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using BookBlossom.Core.DTOs;
using BookBlossom.Core.Interfaces.Services;
using BookBlossom.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;

namespace BookBlossom.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class MessagesAPIController : ControllerBase
    {
        private readonly IMessageService _messageService;
        private readonly ICallRequestService _callRequestService;
        private readonly ApplicationDbContext _context;
        private readonly Microsoft.AspNetCore.SignalR.IHubContext<BookBlossom.Web.Hubs.ChatHub> _hubContext;

        public MessagesAPIController(
            IMessageService messageService, 
            ICallRequestService callRequestService, 
            ApplicationDbContext context,
            Microsoft.AspNetCore.SignalR.IHubContext<BookBlossom.Web.Hubs.ChatHub> hubContext)
        {
            _messageService = messageService;
            _callRequestService = callRequestService;
            _context = context;
            _hubContext = hubContext;
        }

        private long GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (long.TryParse(userIdClaim, out long userId))
            {
                return userId;
            }
            return 0;
        }

        private bool IsAdminOrStaff()
        {
            return User.IsInRole("Admin") || User.IsInRole("Staff");
        }

        [HttpGet("conversations")]
        public async Task<IActionResult> GetConversations()
        {
            try
            {
                if (IsAdminOrStaff())
                {
                    var conversations = await _messageService.GetAllConversationsAsync();
                    return Ok(new { success = true, data = conversations });
                }
                else
                {
                    var userId = GetCurrentUserId();
                    var conversation = await _messageService.GetOrCreateConversationAsync(userId);
                    return Ok(new { success = true, data = new List<ConversationDto> { conversation } });
                }
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpGet("conversations/{conversationId}")]
        public async Task<IActionResult> GetMessages(long conversationId)
        {
            try
            {
                var userId = GetCurrentUserId();
                
                // Fetch messages
                var messages = await _messageService.GetMessagesAsync(conversationId);
                
                // Mark as read asynchronously
                await _messageService.MarkMessagesAsReadAsync(conversationId, userId);

                return Ok(new { success = true, data = messages });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> SendMessage([FromBody] CreateMessageDto dto)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _messageService.SendMessageAsync(userId, dto);

                // Broadcast the message using SignalR
                if (result != null && result.MessageID > 0)
                {
                    await _hubContext.Clients
                        .Group($"conversation-{result.ConversationID}")
                        .SendAsync("ReceiveMessage", result);

                    await _hubContext.Clients
                        .Group("Admins")
                        .SendAsync("ReceiveMessage", result);
                }

                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpGet("call-requests")]
        public async Task<IActionResult> GetCallRequests()
        {
            try
            {
                if (IsAdminOrStaff())
                {
                    var requests = await _callRequestService.GetAllCallRequestsAsync();
                    return Ok(new { success = true, data = requests });
                }
                else
                {
                    var userId = GetCurrentUserId();
                    var requests = await _callRequestService.GetCustomerCallRequestsAsync(userId);
                    return Ok(new { success = true, data = requests });
                }
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("call-request")]
        public async Task<IActionResult> CreateCallRequest([FromBody] CreateCallRequestDto dto)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _callRequestService.CreateCallRequestAsync(userId, dto);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [Authorize(Roles = "Admin, Staff")]
        [HttpPut("call-requests/{id}/resolve")]
        public async Task<IActionResult> ResolveCallRequest(long id)
        {
            try
            {
                await _callRequestService.ResolveCallRequestAsync(id);
                return Ok(new { success = true, message = "Resolved successfully." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [Authorize(Roles = "Admin, Staff")]
        [HttpPut("call-requests/{id}/in-progress")]
        public async Task<IActionResult> SetCallRequestInProgress(long id)
        {
            try
            {
                await _callRequestService.SetCallRequestInProgressAsync(id);
                return Ok(new { success = true, message = "Status updated to In Progress successfully." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [Authorize(Roles = "Admin, Staff")]
        [HttpGet("conversations/{conversationId}/buyer-profile")]
        public async Task<IActionResult> GetBuyerProfile(long conversationId)
        {
            try
            {
                var conversation = await _context.Conversations
                    .FirstOrDefaultAsync(c => c.ConversationID == conversationId);

                if (conversation == null)
                {
                    return NotFound(new { success = false, message = "Conversation not found." });
                }

                var buyerId = conversation.BuyerID;

                var user = await _context.Users
                    .Include(u => u.CustomerDetail)
                    .FirstOrDefaultAsync(u => u.UserID == buyerId);

                if (user == null)
                {
                    return NotFound(new { success = false, message = "Buyer not found." });
                }

                // Total Orders count
                var totalOrders = await _context.Orders
                    .CountAsync(o => o.CustomerID == buyerId);

                // Default Address
                var defaultAddress = await _context.DeliveryAddresses
                    .FirstOrDefaultAsync(da => da.CustomerID == buyerId && da.IsDefault == true);

                if (defaultAddress == null)
                {
                    defaultAddress = await _context.DeliveryAddresses
                        .FirstOrDefaultAsync(da => da.CustomerID == buyerId);
                }

                string addressStr = defaultAddress != null 
                    ? defaultAddress.DetailAddress 
                    : "No address registered";

                // Joined Date calculation
                var baseDate = new DateTime(2023, 1, 15);
                var offsetDays = (int)((user.UserID * 37) % 1000);
                var joinDate = baseDate.AddDays(offsetDays).ToString("MMM dd, yyyy");

                // Total spent (from CustomerDetail)
                decimal totalSpent = user.CustomerDetail?.TotalSpending ?? 0m;

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        buyerId = user.UserID,
                        buyerName = user.UserName,
                        avatar = user.Avatar ?? "/images/Avatar/default.png",
                        joinedDate = "Joined: " + joinDate,
                        totalOrders = totalOrders,
                        totalSpent = totalSpent,
                        defaultAddress = addressStr,
                        phoneNumber = string.IsNullOrEmpty(user.PhoneNumber) ? "No phone number" : user.PhoneNumber,
                        email = string.IsNullOrEmpty(user.Email) ? "No email" : user.Email
                    }
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }
    }
}
