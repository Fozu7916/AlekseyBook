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
        }

        public async Task LeaveChat(string userId)
        {
            if (UserConnections.ContainsKey(Context.ConnectionId))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, userId);
                UserConnections.Remove(Context.ConnectionId);
            }
        }

        public async Task SendTypingStatus(string receiverId, bool isTyping)
        {
            if (UserConnections.TryGetValue(Context.ConnectionId, out string senderId))
            {
                await Clients.Group(receiverId).SendAsync("ReceiveTypingStatus", senderId, isTyping);
            }
            else
            {
                _logger.LogWarning("Попытка отправить статус набора текста от неизвестного пользователя. ConnectionId: {ConnectionId}", 
                    Context.ConnectionId);
            }
        }

        public async Task SendMessage(Message message)
        {
            if (UserConnections.TryGetValue(Context.ConnectionId, out string senderId))
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
                _logger.LogWarning("Попытка отправить сообщение от неизвестного пользователя. ConnectionId: {ConnectionId}", 
                    Context.ConnectionId);
            }
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            if (UserConnections.TryGetValue(Context.ConnectionId, out string userId))
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