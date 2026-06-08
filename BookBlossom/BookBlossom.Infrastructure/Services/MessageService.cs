using System;
using System.IO;
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
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now,
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
                CustomerAvatar = conversation.User?.Avatar ?? "/images/Avatar/default.png",
                UpdatedAt = DateTime.SpecifyKind(conversation.UpdatedAt, DateTimeKind.Local),
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
                    CustomerAvatar = conversation.User?.Avatar ?? "/images/Avatar/default.png",
                    UpdatedAt = DateTime.SpecifyKind(conversation.UpdatedAt, DateTimeKind.Local),
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
                    ConversationID = m.ConversationID,
                    SenderID = m.SenderType == 1 ? conversation.BuyerID : 1,
                    SenderName = m.SenderType == 1 ? (conversation.User?.UserName ?? "Unknown") : "BookBlossom Shop",
                    SenderAvatar = m.SenderType == 1 ? (conversation.User?.Avatar ?? "/images/Avatar/default.png") : "/images/Avatar/admin.png",
                    Content = m.Content,
                    SentAt = DateTime.SpecifyKind(m.CreatedAt, DateTimeKind.Local),
                    IsRead = m.SenderType == 1 ? m.IsReadByShop : m.IsReadByBuyer
                };

                var bookAttachment = m.Attachments.FirstOrDefault(a => a.AttachmentType == 2);
                if (bookAttachment != null)
                {
                    dto.AttachedBookID = bookAttachment.FileSize;
                    dto.AttachedBookTitle = bookAttachment.FileName;
                    dto.AttachedBookAuthor = bookAttachment.FileType;
                    dto.AttachedBookImage = bookAttachment.FileUrl;

                    if (dto.AttachedBookID.HasValue && dto.AttachedBookID.Value > 0 && dto.AttachedBookTitle != null && !dto.AttachedBookTitle.Contains("Blind Book") && !dto.AttachedBookTitle.Contains("Mystery Book"))
                    {
                        var realBook = await _context.RealBooks.Include(b => b.BookAuthors).ThenInclude(ba => ba.Author).Include(b => b.BookImages).FirstOrDefaultAsync(b => b.BookID == dto.AttachedBookID.Value);
                        if (realBook != null)
                        {
                            dto.AttachedBookTitle = realBook.Title;
                            var authorNames = string.Join(", ", realBook.BookAuthors.Select(ba => ba.Author.AuthorName));
                            if (!string.IsNullOrEmpty(authorNames))
                            {
                                dto.AttachedBookAuthor = authorNames;
                            }
                            dto.AttachedBookImage = realBook.BookImages.FirstOrDefault(i => i.IsMain)?.ImagePath ?? realBook.BookImages.FirstOrDefault()?.ImagePath ?? dto.AttachedBookImage;
                        }
                    }
                }

                var mediaAttachments = m.Attachments.Where(a => a.AttachmentType == 1).ToList();
                if (mediaAttachments.Any())
                {
                    dto.AttachmentUrls = mediaAttachments.Select(a => a.FileUrl).ToList();
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
                senderType = 0; // Shop/Admin (0 satisfies check constraint [SenderType]=(1) OR [SenderType]=(0))
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
                IsReadByShop = senderType == 0,
                IsDeleted = false,
                CreatedAt = DateTime.Now
            };

            _context.Messages.Add(message);
            await _context.SaveChangesAsync(); // Save to get MessageID

            if (dto.AttachmentUrls != null && dto.AttachmentUrls.Any())
            {
                foreach (var attachmentUrl in dto.AttachmentUrls)
                {
                    if (string.IsNullOrEmpty(attachmentUrl)) continue;

                    string fileUrlToSave = attachmentUrl;

                    if (attachmentUrl.StartsWith("data:"))
                    {
                        try
                        {
                            var match = System.Text.RegularExpressions.Regex.Match(attachmentUrl, @"data:(?<type>.+?);base64,(?<data>.+)");
                            if (match.Success)
                            {
                                string mimeType = match.Groups["type"].Value;
                                string base64Data = match.Groups["data"].Value;
                                string extension = mimeType.Contains("video") ? ".mp4" : (mimeType.Contains("png") ? ".png" : ".jpg");
                                
                                string fileName = Guid.NewGuid().ToString() + extension;
                                var conversationForBuyer = await _context.Conversations.FindAsync(conversationId);
                                long buyerId = conversationForBuyer?.BuyerID ?? senderId;
                                
                                string uploadDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "chat", $"buyer_{buyerId}");
                                if (!Directory.Exists(uploadDir))
                                {
                                    Directory.CreateDirectory(uploadDir);
                                }
                                
                                string filePath = Path.Combine(uploadDir, fileName);
                                byte[] imageBytes = Convert.FromBase64String(base64Data);
                                await System.IO.File.WriteAllBytesAsync(filePath, imageBytes);
                                
                                fileUrlToSave = $"/uploads/chat/buyer_{buyerId}/{fileName}";
                            }
                        }
                        catch (Exception ex)
                        {
                            // Fallback to original if parsing or saving fails
                        }
                    }

                    _context.MessageAttachments.Add(new MessageAttachment
                    {
                        MessageID = message.MessageID,
                        FileUrl = fileUrlToSave,
                        FileName = "Media",
                        FileType = fileUrlToSave.EndsWith(".mp4") ? "video" : "image",
                        AttachmentType = 1, // Media
                        CreatedAt = DateTime.Now
                    });
                }
            }

            if (dto.AttachedBookID.HasValue && dto.AttachedBookID.Value > 0)
            {
                if (dto.IsBlindDate)
                {
                    _context.MessageAttachments.Add(new MessageAttachment
                    {
                        MessageID = message.MessageID,
                        FileUrl = dto.AttachedBookImage ?? "/images/BlindDateBook/BlindBook.jpg",
                        FileName = dto.AttachedBookTitle ?? "Blind Book",
                        FileType = dto.AttachedBookAuthor ?? "Mystery",
                        FileSize = dto.AttachedBookID.Value,
                        AttachmentType = 2, // Book
                        CreatedAt = DateTime.Now
                    });
                }
                else
                {
                    var book = await _context.RealBooks.Include(b => b.BookAuthors).ThenInclude(ba => ba.Author).Include(b => b.BookImages).FirstOrDefaultAsync(b => b.BookID == dto.AttachedBookID.Value);
                    if (book != null)
                    {
                        _context.MessageAttachments.Add(new MessageAttachment
                        {
                            MessageID = message.MessageID,
                            FileUrl = book.BookImages.FirstOrDefault(i => i.IsMain)?.ImagePath ?? book.BookImages.FirstOrDefault()?.ImagePath ?? "/images/Book/book1.jpg",
                            FileName = book.Title,
                            FileType = string.Join(", ", book.BookAuthors.Select(ba => ba.Author.AuthorName)),
                            FileSize = book.BookID,
                            AttachmentType = 2, // Book
                            CreatedAt = DateTime.Now
                        });
                    }
                }
            }

            var conversation = await _context.Conversations.FindAsync(conversationId);
            if (conversation != null)
            {
                conversation.UpdatedAt = DateTime.Now;
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
                else if (!isShop && message.SenderType == 0 && !message.IsReadByBuyer)
                {
                    message.IsReadByBuyer = true;
                }
            }

            await _context.SaveChangesAsync();
        }
    }
}
