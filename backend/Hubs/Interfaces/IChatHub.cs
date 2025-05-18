using backend.Models;

namespace backend.Hubs.Interfaces
{
    public interface IChatHub
    {
        Task JoinChat(string userId);
        Task LeaveChat(string userId);
        Task SendTypingStatus(string receiverId, bool isTyping);
        Task SendMessage(Message message);
        Task UpdateMessageStatus(int messageId, bool isRead);
    }
} 