using System;

namespace backend.Models.DTOs
{
    public class MessageDto
    {
        public int Id { get; set; }
        public UserResponseDto Sender { get; set; }
        public UserResponseDto Receiver { get; set; }
        public string Content { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class SendMessageDto
    {
        public int ReceiverId { get; set; }
        public string Content { get; set; }
    }

    public class ChatPreviewDto
    {
        public UserResponseDto User { get; set; }
        public MessageDto LastMessage { get; set; }
        public int UnreadCount { get; set; }
    }
} 