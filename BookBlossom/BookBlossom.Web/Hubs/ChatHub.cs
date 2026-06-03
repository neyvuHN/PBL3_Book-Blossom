using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace BookBlossom.Web.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        public async Task JoinConversation(long conversationId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"conversation-{conversationId}");
        }

        public async Task LeaveConversation(long conversationId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"conversation-{conversationId}");
        }

        public async Task JoinAdminGroup()
        {
            if (Context.User != null && (Context.User.IsInRole("Admin") || Context.User.IsInRole("Staff")))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, "Admins");
            }
        }
    }
}
