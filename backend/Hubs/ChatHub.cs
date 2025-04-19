using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;
using backend.Models;

namespace backend.Hubs
{
    public class ChatHub : Hub
    {
        private static Dictionary<string, string> UserConnections = new Dictionary<string, string>();

        public async Task JoinChat(string userId)
        {
            UserConnections[Context.ConnectionId] = userId;
            await Groups.AddToGroupAsync(Context.ConnectionId, userId);
            Console.WriteLine($"User {userId} joined chat. Connection ID: {Context.ConnectionId}");
        }

        public async Task LeaveChat(string userId)
        {
            if (UserConnections.ContainsKey(Context.ConnectionId))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, userId);
                UserConnections.Remove(Context.ConnectionId);
                Console.WriteLine($"User {userId} left chat. Connection ID: {Context.ConnectionId}");
            }
        }

        public async Task SendTypingStatus(string receiverId, bool isTyping)
        {
            if (UserConnections.TryGetValue(Context.ConnectionId, out string senderId))
            {
                Console.WriteLine($"Typing status from {senderId} to {receiverId}: {isTyping}");
                await Clients.Group(receiverId).SendAsync("ReceiveTypingStatus", senderId, isTyping);
            }
            else
            {
                Console.WriteLine($"User not found for connection {Context.ConnectionId}");
            }
        }

        public async Task SendMessage(Message message)
        {
            if (UserConnections.TryGetValue(Context.ConnectionId, out string senderId))
            {
                Console.WriteLine($"Sending message from {senderId} to {message.ReceiverId}");
                await Clients.Group(message.ReceiverId.ToString()).SendAsync("ReceiveMessage", message);
            }
            else
            {
                Console.WriteLine($"User not found for connection {Context.ConnectionId}");
            }
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            if (UserConnections.TryGetValue(Context.ConnectionId, out string userId))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, userId);
                UserConnections.Remove(Context.ConnectionId);
                Console.WriteLine($"User {userId} disconnected. Connection ID: {Context.ConnectionId}");
            }
            await base.OnDisconnectedAsync(exception);
        }
    }
} 