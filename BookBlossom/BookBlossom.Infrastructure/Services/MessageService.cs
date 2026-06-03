using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BookBlossom.Core.DTOs;
using BookBlossom.Core.Entities;
using BookBlossom.Core.Enums;
using BookBlossom.Core.Interfaces.Services;
using BookBlossom.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BookBlossom.Infrastructure.Services
{
    public class MessageService : IMessageService
    {
        private readonly ApplicationDbContext _context;

        public MessageService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ConversationDto> GetOrCreateConversationAsync(long customerId)
        {
            var conversation = await _context.Conversations
                .Include(c => c.User)
                .ThenInclude(u => u.Role)
                .FirstOrDefaultAsync(c => c.CustomerID == customerId);

            if (conversation == null)
            {
                var customer = await _context.Users.FindAsync(customerId);
                if (customer == null) throw new Exception("Customer not found.");

                conversation = new Conversation
                {
                    CustomerID = customerId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.Conversations.Add(conversation);
                await _context.SaveChangesAsync();
            }

            var lastMessage = await _context.Messages
                .Where(m => m.ConversationID == conversation.ConversationID)
                .OrderByDescending(m => m.SentAt)
                .FirstOrDefaultAsync();

            var unreadCount = await _context.Messages
                .CountAsync(m => m.ConversationID == conversation.ConversationID && !m.IsRead && m.SenderID != customerId);

            return new ConversationDto
            {
                ConversationID = conversation.ConversationID,
                CustomerID = conversation.CustomerID,
                CustomerName = conversation.User?.UserName ?? "Unknown",
                CustomerAvatar = "/images/Avatar/default-avatar.png", // Assuming default for now
                UpdatedAt = conversation.UpdatedAt,
                LastMessageSnippet = lastMessage != null ? (lastMessage.Content.Length > 30 ? lastMessage.Content.Substring(0, 30) + "..." : lastMessage.Content) : "",
                HasUnreadMessages = unreadCount > 0
            };
        }

        public async Task<List<ConversationDto>> GetAllConversationsAsync()
        {
            var conversations = await _context.Conversations
                .Include(c => c.User)
                .OrderByDescending(c => c.UpdatedAt)
                .ToListAsync();

            var dtos = new List<ConversationDto>();
            foreach (var conversation in conversations)
            {
                var lastMessage = await _context.Messages
                    .Where(m => m.ConversationID == conversation.ConversationID)
                    .OrderByDescending(m => m.SentAt)
                    .FirstOrDefaultAsync();

                // Unread messages for admin (sent by customer, not read by admin)
                var unreadCount = await _context.Messages
                    .CountAsync(m => m.ConversationID == conversation.ConversationID && !m.IsRead && m.SenderID == conversation.CustomerID);

                dtos.Add(new ConversationDto
                {
                    ConversationID = conversation.ConversationID,
                    CustomerID = conversation.CustomerID,
                    CustomerName = conversation.User?.UserName ?? "Unknown",
                    CustomerAvatar = "/images/Avatar/default-avatar.png",
                    UpdatedAt = conversation.UpdatedAt,
                    LastMessageSnippet = lastMessage != null ? (lastMessage.Content.Length > 30 ? lastMessage.Content.Substring(0, 30) + "..." : lastMessage.Content) : "",
                    HasUnreadMessages = unreadCount > 0
                });
            }

            return dtos;
        }

        public async Task<List<MessageDto>> GetMessagesAsync(long conversationId)
        {
            var messages = await _context.Messages
                .Include(m => m.Sender)
                .ThenInclude(u => u.Role)
                .Include(m => m.AttachedBook)
                .Where(m => m.ConversationID == conversationId)
                .OrderBy(m => m.SentAt)
                .ToListAsync();

            return messages.Select(m => new MessageDto
            {
                MessageID = m.MessageID,
                SenderID = m.SenderID,
                SenderName = m.Sender?.UserName ?? "Unknown",
                SenderAvatar = m.Sender?.RoleID == UserRole.Admin ? "/images/Avatar/admin.png" : "/images/Avatar/default-avatar.png",
                Content = m.Content,
                AttachmentUrl = m.AttachmentUrl,
                AttachedBookID = m.AttachedBookID,
                AttachedBookTitle = m.AttachedBook?.Title,
                AttachedBookAuthor = string.Join(", ", m.AttachedBook?.BookAuthors?.Select(ba => ba.Author?.AuthorName ?? "") ?? new List<string>()),
                AttachedBookImage = "/images/Book/book1.jpg",
                SentAt = m.SentAt,
                IsRead = m.IsRead
            }).ToList();
        }

        public async Task<MessageDto> SendMessageAsync(long senderId, CreateMessageDto dto)
        {
            long conversationId = 0;
            
            if (dto.ConversationID.HasValue && dto.ConversationID.Value > 0)
            {
                conversationId = dto.ConversationID.Value;
            }
            else if (dto.ReceiverID.HasValue) // Admin sending to specific customer or Customer initializing
            {
                var customerId = dto.ReceiverID.Value;
                // Check if sender is customer, if so, customerId is senderId
                var sender = await _context.Users.FindAsync(senderId);
                if (sender != null && sender.RoleID == UserRole.Guest || sender.RoleID == UserRole.Customer)
                {
                     customerId = senderId;
                }
                var conv = await GetOrCreateConversationAsync(customerId);
                conversationId = conv.ConversationID;
            }
            else
            {
                // Customer sending
                var conv = await GetOrCreateConversationAsync(senderId);
                conversationId = conv.ConversationID;
            }

            var message = new Message
            {
                ConversationID = conversationId,
                SenderID = senderId,
                Content = dto.Content,
                AttachmentUrl = dto.AttachmentUrl,
                AttachedBookID = dto.AttachedBookID,
                SentAt = DateTime.UtcNow,
                IsRead = false
            };

            _context.Messages.Add(message);

            var conversation = await _context.Conversations.FindAsync(conversationId);
            if (conversation != null)
            {
                conversation.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            // Load navigation properties for DTO
            var m = await _context.Messages
                .Include(msg => msg.Sender)
                .Include(msg => msg.AttachedBook)
                .FirstOrDefaultAsync(msg => msg.MessageID == message.MessageID);

            return new MessageDto
            {
                MessageID = m.MessageID,
                SenderID = m.SenderID,
                SenderName = m.Sender?.UserName ?? "Unknown",
                SenderAvatar = m.Sender?.RoleID == UserRole.Admin ? "/images/Avatar/admin.png" : "/images/Avatar/default-avatar.png",
                Content = m.Content,
                AttachmentUrl = m.AttachmentUrl,
                AttachedBookID = m.AttachedBookID,
                AttachedBookTitle = m.AttachedBook?.Title,
                AttachedBookAuthor = string.Join(", ", m.AttachedBook?.BookAuthors?.Select(ba => ba.Author?.AuthorName ?? "") ?? new List<string>()),
                AttachedBookImage = "/images/Book/book1.jpg",
                SentAt = m.SentAt,
                IsRead = m.IsRead
            };
        }

        public async Task MarkMessagesAsReadAsync(long conversationId, long readerId)
        {
            var messages = await _context.Messages
                .Where(m => m.ConversationID == conversationId && m.SenderID != readerId && !m.IsRead)
                .ToListAsync();

            foreach (var message in messages)
            {
                message.IsRead = true;
            }

            await _context.SaveChangesAsync();
        }
    }
}
