using System;

namespace backend.Models.DTOs
{
    public class MessageDto
    {
        public int Id { get; set; }
        public required UserResponseDto Sender { get; set; }
        public required UserResponseDto Receiver { get; set; }
        public required string Content { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class SendMessageDto
    {
        public int ReceiverId { get; set; }
        public required string Content { get; set; }
    }

    public class ChatPreviewDto
    {
        public required UserResponseDto User { get; set; }
        public required MessageDto LastMessage { get; set; }
        public int UnreadCount { get; set; }
    }
} 