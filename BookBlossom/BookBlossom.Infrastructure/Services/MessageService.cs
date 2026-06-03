using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using BookBlossom.Core.Entities;
using BookBlossom.Core.Interfaces.Services;
using BookBlossom.Core.DTOs;
using BookBlossom.Infrastructure.Data;
using BookBlossom.Core.Enums;

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
                .FirstOrDefaultAsync(c => c.BuyerID == customerId);

            if (conversation == null)
            {
                var customer = await _context.Users.FindAsync(customerId);
                if (customer == null) throw new Exception("Customer not found.");

                conversation = new Conversation
                {
                    BuyerID = customerId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IsClosed = false
                };
                _context.Conversations.Add(conversation);
                await _context.SaveChangesAsync();
            }

            var lastMessage = await _context.Messages
                .Where(m => m.ConversationID == conversation.ConversationID)
                .OrderByDescending(m => m.CreatedAt)
                .FirstOrDefaultAsync();

            var unreadCount = await _context.Messages
                .CountAsync(m => m.ConversationID == conversation.ConversationID && !m.IsReadByBuyer && m.SenderType != 1);

            return new ConversationDto
            {
                ConversationID = conversation.ConversationID,
                CustomerID = conversation.BuyerID,
                CustomerName = conversation.User?.UserName ?? "Unknown",
                CustomerAvatar = "/images/Avatar/default-avatar.png",
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
                    .OrderByDescending(m => m.CreatedAt)
                    .FirstOrDefaultAsync();

                var unreadCount = await _context.Messages
                    .CountAsync(m => m.ConversationID == conversation.ConversationID && !m.IsReadByShop && m.SenderType == 1);

                dtos.Add(new ConversationDto
                {
                    ConversationID = conversation.ConversationID,
                    CustomerID = conversation.BuyerID,
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
            var conversation = await _context.Conversations.Include(c => c.User).FirstOrDefaultAsync(c => c.ConversationID == conversationId);
            if(conversation == null) return new List<MessageDto>();

            var messages = await _context.Messages
                .Include(m => m.Attachments)
                .Where(m => m.ConversationID == conversationId)
                .OrderBy(m => m.CreatedAt)
                .ToListAsync();

            var result = new List<MessageDto>();
            foreach(var m in messages)
            {
                var dto = new MessageDto
                {
                    MessageID = m.MessageID,
                    SenderID = m.SenderType == 1 ? conversation.BuyerID : 1,
                    SenderName = m.SenderType == 1 ? (conversation.User?.UserName ?? "Unknown") : "BookBlossom Shop",
                    SenderAvatar = m.SenderType == 1 ? "/images/Avatar/default-avatar.png" : "/images/Avatar/admin.png",
                    Content = m.Content,
                    SentAt = m.CreatedAt,
                    IsRead = m.SenderType == 1 ? m.IsReadByShop : m.IsReadByBuyer
                };

                var bookAttachment = m.Attachments.FirstOrDefault(a => a.AttachmentType == 2);
                if (bookAttachment != null)
                {
                    dto.AttachedBookID = bookAttachment.FileSize;
                    dto.AttachedBookTitle = bookAttachment.FileName;
                    dto.AttachedBookAuthor = bookAttachment.FileType;
                    dto.AttachedBookImage = bookAttachment.FileUrl;
                }

                var mediaAttachment = m.Attachments.FirstOrDefault(a => a.AttachmentType == 1);
                if (mediaAttachment != null)
                {
                    dto.AttachmentUrl = mediaAttachment.FileUrl;
                }

                result.Add(dto);
            }
            return result;
        }

        public async Task<MessageDto> SendMessageAsync(long senderId, CreateMessageDto dto)
        {
            long conversationId = 0;
            byte senderType = 1; // Default to Buyer
            
            var sender = await _context.Users.FindAsync(senderId);
            if (sender != null && (sender.RoleID == UserRole.Admin || false))
            {
                senderType = 2; // Shop/Admin
            }

            if (dto.ConversationID.HasValue && dto.ConversationID.Value > 0)
            {
                conversationId = dto.ConversationID.Value;
            }
            else if (dto.ReceiverID.HasValue && senderType == 2) 
            {
                var customerId = dto.ReceiverID.Value;
                var conv = await GetOrCreateConversationAsync(customerId);
                conversationId = conv.ConversationID;
            }
            else
            {
                var conv = await GetOrCreateConversationAsync(senderId);
                conversationId = conv.ConversationID;
            }

            var message = new Message
            {
                ConversationID = conversationId,
                SenderType = senderType,
                Content = dto.Content ?? "",
                MessageType = 1,
                IsReadByBuyer = senderType == 1,
                IsReadByShop = senderType == 2,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.Messages.Add(message);
            await _context.SaveChangesAsync(); // Save to get MessageID

            if (!string.IsNullOrEmpty(dto.AttachmentUrl))
            {
                _context.MessageAttachments.Add(new MessageAttachment
                {
                    MessageID = message.MessageID,
                    FileUrl = dto.AttachmentUrl,
                    FileName = "Media",
                    FileType = dto.AttachmentUrl.Contains(".mp4") ? "video" : "image",
                    AttachmentType = 1, // Media
                    CreatedAt = DateTime.UtcNow
                });
            }

            if (dto.AttachedBookID.HasValue && dto.AttachedBookID.Value > 0)
            {
                var book = await _context.RealBooks.Include(b => b.BookAuthors).ThenInclude(ba => ba.Author).FirstOrDefaultAsync(b => b.BookID == dto.AttachedBookID.Value);
                if (book != null)
                {
                    _context.MessageAttachments.Add(new MessageAttachment
                    {
                        MessageID = message.MessageID,
                        FileUrl = "/images/Book/book1.jpg", // Hardcoded as original or could be fetched separately
                        FileName = book.Title,
                        FileType = string.Join(", ", book.BookAuthors.Select(ba => ba.Author.AuthorName)),
                        FileSize = book.BookID,
                        AttachmentType = 2, // Book
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }

            var conversation = await _context.Conversations.FindAsync(conversationId);
            if (conversation != null)
            {
                conversation.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            var resultDtos = await GetMessagesAsync(conversationId);
            return resultDtos.LastOrDefault() ?? new MessageDto();
        }

        public async Task MarkMessagesAsReadAsync(long conversationId, long readerId)
        {
            var sender = await _context.Users.FindAsync(readerId);
            bool isShop = sender != null && (sender.RoleID == UserRole.Admin || false);

            var messages = await _context.Messages
                .Where(m => m.ConversationID == conversationId)
                .ToListAsync();

            foreach (var message in messages)
            {
                if (isShop && message.SenderType == 1 && !message.IsReadByShop)
                {
                    message.IsReadByShop = true;
                }
                else if (!isShop && message.SenderType == 2 && !message.IsReadByBuyer)
                {
                    message.IsReadByBuyer = true;
                }
            }

            await _context.SaveChangesAsync();
        }
    }
}
