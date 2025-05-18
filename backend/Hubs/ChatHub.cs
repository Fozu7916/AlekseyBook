using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;
using backend.Models;
using Microsoft.Extensions.Logging;
using backend.Hubs.Interfaces;

namespace backend.Hubs
{
    public class ChatHub : Hub, IChatHub
    {
        private static readonly Dictionary<string, string> UserConnections = new();
        private readonly ILogger<ChatHub> _logger;

        public ChatHub(ILogger<ChatHub> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task JoinChat(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                throw new ArgumentNullException(nameof(userId));
            }

            UserConnections[Context.ConnectionId] = userId;
            await Groups.AddToGroupAsync(Context.ConnectionId, userId);
        }

        public async Task LeaveChat(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                throw new ArgumentNullException(nameof(userId));
            }

            if (UserConnections.ContainsKey(Context.ConnectionId))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, userId);
                UserConnections.Remove(Context.ConnectionId);
            }
        }

        public async Task SendTypingStatus(string receiverId, bool isTyping)
        {
            if (string.IsNullOrEmpty(receiverId))
            {
                throw new ArgumentNullException(nameof(receiverId));
            }

            if (UserConnections.TryGetValue(Context.ConnectionId, out string? senderId) && senderId != null)
            {
                await Clients.Group(receiverId).SendAsync("ReceiveTypingStatus", senderId, isTyping);
            }
            else
            {
                _logger.LogError("Попытка отправить статус набора текста от неавторизованного пользователя. ConnectionId: {ConnectionId}", 
                    Context.ConnectionId);
            }
        }

        public async Task SendMessage(Message message)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            if (UserConnections.TryGetValue(Context.ConnectionId, out string? senderId) && senderId != null)
            {
                try 
                {
                    await Clients.Group(message.ReceiverId.ToString()).SendAsync("ReceiveMessage", message);
                    await Clients.Group(senderId).SendAsync("ReceiveMessage", message);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ошибка при отправке сообщения от {SenderId} к {ReceiverId}", 
                        senderId, message.ReceiverId);
                    throw;
                }
            }
            else
            {
                _logger.LogError("Попытка отправить сообщение от неавторизованного пользователя. ConnectionId: {ConnectionId}", 
                    Context.ConnectionId);
            }
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            if (UserConnections.TryGetValue(Context.ConnectionId, out string? userId) && userId != null)
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, userId);
                UserConnections.Remove(Context.ConnectionId);
                
                if (exception != null)
                {
                    _logger.LogError(exception, "Пользователь {UserId} отключился с ошибкой", userId);
                }
            }
            await base.OnDisconnectedAsync(exception);
        }
    }
} 