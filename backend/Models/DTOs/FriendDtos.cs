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
        public required UserResponseDto User { get; set; }
        public required UserResponseDto Friend { get; set; }
        public required string Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class FriendListResponseDto
    {
        public FriendListResponseDto()
        {
            Friends = new List<UserResponseDto>();
            PendingRequests = new List<UserResponseDto>();
            SentRequests = new List<UserResponseDto>();
        }

        public required List<UserResponseDto> Friends { get; set; }
        public required List<UserResponseDto> PendingRequests { get; set; }
        public required List<UserResponseDto> SentRequests { get; set; }
    }
} 