using backend.Models.DTOs;

namespace backend.Services.Interfaces
{
    public interface IMessageService
    {
        Task<MessageDto> SendMessage(int senderId, SendMessageDto messageDto);
        Task<List<MessageDto>> GetChatMessages(int userId, int otherUserId);
        Task<List<ChatPreviewDto>> GetUserChats(int userId);
        Task MarkMessagesAsRead(int userId, int otherUserId);
        Task<int> GetUnreadMessagesCount(int userId);
    }
} 