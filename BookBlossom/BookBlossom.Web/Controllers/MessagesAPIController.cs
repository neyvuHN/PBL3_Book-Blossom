using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using BookBlossom.Core.DTOs;
using BookBlossom.Core.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookBlossom.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class MessagesAPIController : ControllerBase
    {
        private readonly IMessageService _messageService;
        private readonly ICallRequestService _callRequestService;

        public MessagesAPIController(IMessageService messageService, ICallRequestService callRequestService)
        {
            _messageService = messageService;
            _callRequestService = callRequestService;
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
    }
}
