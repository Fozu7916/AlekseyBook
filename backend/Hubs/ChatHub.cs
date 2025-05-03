using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;
using backend.Models;
using Microsoft.Extensions.Logging;

namespace backend.Hubs
{
    public class ChatHub : Hub
    {
        private static Dictionary<string, string> UserConnections = new Dictionary<string, string>();
        private readonly ILogger<ChatHub> _logger;

        public ChatHub(ILogger<ChatHub> logger)
        {
            _logger = logger;
        }

        public async Task JoinChat(string userId)
        {
            UserConnections[Context.ConnectionId] = userId;
            await Groups.AddToGroupAsync(Context.ConnectionId, userId);
            _logger.LogInformation("User {UserId} joined chat. Connection ID: {ConnectionId}", 
                userId, Context.ConnectionId);
        }

        public async Task LeaveChat(string userId)
        {
            if (UserConnections.ContainsKey(Context.ConnectionId))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, userId);
                UserConnections.Remove(Context.ConnectionId);
                _logger.LogInformation("User {UserId} left chat. Connection ID: {ConnectionId}", 
                    userId, Context.ConnectionId);
            }
        }

        public async Task SendTypingStatus(string receiverId, bool isTyping)
        {
            if (UserConnections.TryGetValue(Context.ConnectionId, out string senderId))
            {
                _logger.LogDebug("Typing status from {SenderId} to {ReceiverId}: {IsTyping}", 
                    senderId, receiverId, isTyping);
                await Clients.Group(receiverId).SendAsync("ReceiveTypingStatus", senderId, isTyping);
            }
            else
            {
                _logger.LogWarning("User not found for connection {ConnectionId}", Context.ConnectionId);
            }
        }

        public async Task SendMessage(Message message)
        {
            if (UserConnections.TryGetValue(Context.ConnectionId, out string senderId))
            {
                _logger.LogInformation("Sending message from {SenderId} to {ReceiverId}", 
                    senderId, message.ReceiverId);
                await Clients.Group(message.ReceiverId.ToString()).SendAsync("ReceiveMessage", message);
                await Clients.Group(senderId).SendAsync("ReceiveMessage", message);
            }
            else
            {
                _logger.LogWarning("User not found for connection {ConnectionId}", Context.ConnectionId);
            }
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            if (UserConnections.TryGetValue(Context.ConnectionId, out string userId))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, userId);
                UserConnections.Remove(Context.ConnectionId);
                _logger.LogInformation("User {UserId} disconnected. Connection ID: {ConnectionId}", 
                    userId, Context.ConnectionId);
            }
            await base.OnDisconnectedAsync(exception);
        }
    }
} 