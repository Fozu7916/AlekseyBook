using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;
using backend.Models;
using Microsoft.Extensions.Logging;
using backend.Hubs.Interfaces;
using backend.Services.Interfaces;

namespace backend.Hubs
{
    public class ChatHub : Hub, IChatHub
    {
        private static readonly Dictionary<string, string> UserConnections = new();
        private readonly ILogger<ChatHub> _logger;
        private readonly IMessageService _messageService;

        public ChatHub(ILogger<ChatHub> logger, IMessageService messageService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));
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
                    var recipientGroups = new[] { message.ReceiverId.ToString(), senderId };
                    await Clients.Groups(recipientGroups).SendAsync("ReceiveMessage", message);
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

        public async Task UpdateMessageStatus(int messageId, bool isRead)
        {
            try
            {
                var userId = UserConnections[Context.ConnectionId];
                if (string.IsNullOrEmpty(userId))
                {
                    throw new InvalidOperationException("Пользователь не найден");
                }

                var message = await _messageService.GetMessageById(messageId);
                if (message == null)
                {
                    throw new InvalidOperationException("Сообщение не найдено");
                }

                if (message.ReceiverId != int.Parse(userId))
                {
                    throw new InvalidOperationException("Нет прав на изменение статуса этого сообщения");
                }

                await _messageService.MarkMessagesAsRead(int.Parse(userId), message.SenderId);
                
                var allMessages = await _messageService.GetChatMessages(int.Parse(userId), message.SenderId);
                var recipientGroups = new[] { userId, message.SenderId.ToString() };
                
                foreach (var msg in allMessages.Where(m => m.Sender.Id == message.SenderId))
                {
                    await Clients.Groups(recipientGroups).SendAsync("ReceiveMessageStatusUpdate", msg.Id, true);
                }

                await Clients.Groups(recipientGroups).SendAsync("UpdateChatList");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при обновлении статуса сообщения");
                throw;
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