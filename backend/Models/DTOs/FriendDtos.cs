using System;

namespace backend.Models.DTOs
{
    public class FriendRequestDto
    {
        public int UserId { get; set; }
        public int FriendId { get; set; }
    }

    public class FriendResponseDto
    {
        public int Id { get; set; }
        public UserResponseDto User { get; set; }
        public UserResponseDto Friend { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class FriendListResponseDto
    {
        public List<UserResponseDto> Friends { get; set; }
        public List<UserResponseDto> PendingRequests { get; set; }
        public List<UserResponseDto> SentRequests { get; set; }
    }
} 