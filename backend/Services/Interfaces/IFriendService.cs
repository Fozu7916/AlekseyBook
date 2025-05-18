using backend.Models.DTOs;

namespace backend.Services.Interfaces
{
    public interface IFriendService
    {
        Task<FriendResponseDto> SendFriendRequest(int userId, int friendId);
        Task<FriendResponseDto> AcceptFriendRequest(int userId, int friendId);
        Task<bool> DeclineFriendRequest(int userId, int friendId);
        Task<bool> RemoveFriend(int userId, int friendId);
        Task<bool> BlockUser(int userId, int blockedUserId);
        Task<FriendListResponseDto> GetFriendsList(int userId);
        Task<bool> IsFriend(int userId, int friendId);
        Task<List<UserResponseDto>> GetUserFriendsList(int userId);
    }
} 