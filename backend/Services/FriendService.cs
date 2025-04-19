using backend.Data;
using backend.Models;
using backend.Models.DTOs;
using Microsoft.EntityFrameworkCore;

namespace backend.Services
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

    public class FriendService : IFriendService
    {
        private readonly ApplicationDbContext _context;
        private readonly IUserService _userService;

        public FriendService(ApplicationDbContext context, IUserService userService)
        {
            _context = context;
            _userService = userService;
        }

        public async Task<FriendResponseDto> SendFriendRequest(int userId, int friendId)
        {
            if (userId == friendId)
                throw new Exception("Нельзя добавить себя в друзья");

            var existingFriendship = await _context.Friends
                .FirstOrDefaultAsync(f => 
                    (f.UserId == userId && f.FriendId == friendId) ||
                    (f.UserId == friendId && f.FriendId == userId));

            if (existingFriendship != null)
            {
                if (existingFriendship.Status == FriendStatus.Blocked)
                    throw new Exception("Невозможно отправить запрос");
                
                if (existingFriendship.Status == FriendStatus.Accepted)
                    throw new Exception("Пользователь уже в списке друзей");
                
                if (existingFriendship.Status == FriendStatus.Pending)
                    throw new Exception("Запрос уже отправлен");
            }

            var friendship = new Friend
            {
                UserId = userId,
                FriendId = friendId,
                Status = FriendStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Friends.Add(friendship);
            await _context.SaveChangesAsync();

            return await MapToFriendResponseDto(friendship);
        }

        public async Task<FriendResponseDto> AcceptFriendRequest(int userId, int friendId)
        {
            var friendRequest = await _context.Friends
                .FirstOrDefaultAsync(f => 
                    f.UserId == friendId && 
                    f.FriendId == userId && 
                    f.Status == FriendStatus.Pending);

            if (friendRequest == null)
                throw new Exception("Запрос в друзья не найден");

            friendRequest.Status = FriendStatus.Accepted;
            friendRequest.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return await MapToFriendResponseDto(friendRequest);
        }

        public async Task<bool> DeclineFriendRequest(int userId, int friendId)
        {
            var friendRequest = await _context.Friends
                .FirstOrDefaultAsync(f => 
                    f.UserId == friendId && 
                    f.FriendId == userId && 
                    f.Status == FriendStatus.Pending);

            if (friendRequest == null)
                return false;

            friendRequest.Status = FriendStatus.Declined;
            friendRequest.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RemoveFriend(int userId, int friendId)
        {
            var friendship = await _context.Friends
                .FirstOrDefaultAsync(f => 
                    (f.UserId == userId && f.FriendId == friendId) ||
                    (f.UserId == friendId && f.FriendId == userId));

            if (friendship == null)
                return false;

            _context.Friends.Remove(friendship);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> BlockUser(int userId, int blockedUserId)
        {
            var existingFriendship = await _context.Friends
                .FirstOrDefaultAsync(f => 
                    (f.UserId == userId && f.FriendId == blockedUserId) ||
                    (f.UserId == blockedUserId && f.FriendId == userId));

            if (existingFriendship != null)
            {
                existingFriendship.Status = FriendStatus.Blocked;
                existingFriendship.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                _context.Friends.Add(new Friend
                {
                    UserId = userId,
                    FriendId = blockedUserId,
                    Status = FriendStatus.Blocked,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<FriendListResponseDto> GetFriendsList(int userId)
        {
            var friends = await _context.Friends
                .Include(f => f.User)
                .Include(f => f.FriendUser)
                .Where(f => 
                    (f.UserId == userId || f.FriendId == userId) &&
                    f.Status != FriendStatus.Blocked)
                .ToListAsync();

            var acceptedFriends = friends
                .Where(f => f.Status == FriendStatus.Accepted)
                .Select(f => f.UserId == userId ? f.FriendUser : f.User)
                .Select(u => MapToUserResponseDto(u))
                .ToList();

            var pendingRequests = friends
                .Where(f => f.Status == FriendStatus.Pending && f.FriendId == userId)
                .Select(f => MapToUserResponseDto(f.User))
                .ToList();

            var sentRequests = friends
                .Where(f => f.Status == FriendStatus.Pending && f.UserId == userId)
                .Select(f => MapToUserResponseDto(f.FriendUser))
                .ToList();

            return new FriendListResponseDto
            {
                Friends = acceptedFriends,
                PendingRequests = pendingRequests,
                SentRequests = sentRequests
            };
        }

        public async Task<bool> IsFriend(int userId, int friendId)
        {
            return await _context.Friends
                .AnyAsync(f => 
                    ((f.UserId == userId && f.FriendId == friendId) ||
                     (f.UserId == friendId && f.FriendId == userId)) &&
                    f.Status == FriendStatus.Accepted);
        }

        public async Task<List<UserResponseDto>> GetUserFriendsList(int userId)
        {
            var friends = await _context.Friends
                .Include(f => f.User)
                .Include(f => f.FriendUser)
                .Where(f => 
                    (f.UserId == userId || f.FriendId == userId) &&
                    f.Status == FriendStatus.Accepted)
                .ToListAsync();

            return friends
                .Select(f => f.UserId == userId ? 
                    MapToUserResponseDto(f.FriendUser) : 
                    MapToUserResponseDto(f.User))
                .ToList();
        }

        private async Task<FriendResponseDto> MapToFriendResponseDto(Friend friend)
        {
            var user = await _userService.GetUserByIdAsync(friend.UserId);
            var friendUser = await _userService.GetUserByIdAsync(friend.FriendId);

            return new FriendResponseDto
            {
                Id = friend.Id,
                User = MapToUserResponseDto(user),
                Friend = MapToUserResponseDto(friendUser),
                Status = friend.Status.ToString().ToLower(),
                CreatedAt = friend.CreatedAt,
                UpdatedAt = friend.UpdatedAt
            };
        }

        private UserResponseDto MapToUserResponseDto(User user)
        {
            return new UserResponseDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                AvatarUrl = user.AvatarUrl,
                Status = user.Status,
                Bio = user.Bio,
                IsVerified = user.IsVerified,
                CreatedAt = user.CreatedAt,
                LastLogin = user.LastLogin
            };
        }
    }
} 