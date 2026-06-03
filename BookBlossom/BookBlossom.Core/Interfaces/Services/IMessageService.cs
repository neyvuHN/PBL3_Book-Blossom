using System.Collections.Generic;
using System.Threading.Tasks;
using BookBlossom.Core.DTOs;

namespace BookBlossom.Core.Interfaces.Services
{
    public interface IMessageService
    {
        Task<ConversationDto> GetOrCreateConversationAsync(long customerId);
        Task<List<ConversationDto>> GetAllConversationsAsync(); // For Admin
        Task<List<MessageDto>> GetMessagesAsync(long conversationId);
        Task<MessageDto> SendMessageAsync(long senderId, CreateMessageDto dto);
        Task MarkMessagesAsReadAsync(long conversationId, long readerId);
    }
}
