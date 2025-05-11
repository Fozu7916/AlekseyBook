using Xunit;
using Moq;
using backend.Services;
using backend.Models;
using backend.Models.DTOs;
using backend.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace backend.UnitTests.Services
{
    public class FriendServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly Mock<IUserService> _userServiceMock;
        private readonly FriendService _friendService;

        public FriendServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "TestFriendServiceDb")
                .Options;
            _context = new ApplicationDbContext(options);

            _userServiceMock = new Mock<IUserService>();

            _friendService = new FriendService(
                _context,
                _userServiceMock.Object
            );
        }

        [Fact]
        public async Task SendFriendRequest_ValidRequest_ReturnsFriendship()
        {
            // Arrange
            var user1 = new User
            {
                Id = 1,
                Username = "user1",
                Email = "user1@example.com",
                Status = "Active",
                PasswordHash = "hash1"
            };
            var user2 = new User
            {
                Id = 2,
                Username = "user2",
                Email = "user2@example.com",
                Status = "Active",
                PasswordHash = "hash2"
            };

            _userServiceMock.Setup(x => x.GetUserByIdAsync(1)).ReturnsAsync(user1);
            _userServiceMock.Setup(x => x.GetUserByIdAsync(2)).ReturnsAsync(user2);

            // Act
            var result = await _friendService.SendFriendRequest(1, 2);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.User.Id);
            Assert.Equal(2, result.Friend.Id);
            Assert.Equal("pending", result.Status.ToString().ToLower());

            var friendshipInDb = await _context.Friends
                .FirstOrDefaultAsync(f => f.UserId == 1 && f.FriendId == 2);
            Assert.NotNull(friendshipInDb);
            Assert.Equal("pending", friendshipInDb.Status.ToString().ToLower());
        }

        [Fact]
        public async Task AcceptFriendRequest_ValidRequest_ReturnsFriendship()
        {
            // Arrange
            var user1 = new User
            {
                Id = 1,
                Username = "user1",
                Email = "user1@example.com",
                Status = "Active",
                PasswordHash = "hash1"
            };
            var user2 = new User
            {
                Id = 2,
                Username = "user2",
                Email = "user2@example.com",
                Status = "Active",
                PasswordHash = "hash2"
            };

            _userServiceMock.Setup(x => x.GetUserByIdAsync(1)).ReturnsAsync(user1);
            _userServiceMock.Setup(x => x.GetUserByIdAsync(2)).ReturnsAsync(user2);

            var friend = new Friend
            {
                UserId = 1,
                FriendId = 2,
                Status = FriendStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                User = user1,
                FriendUser = user2
            };
            await _context.Friends.AddAsync(friend);
            await _context.SaveChangesAsync();

            // Act
            var result = await _friendService.AcceptFriendRequest(2, 1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("accepted", result.Status.ToString().ToLower());

            var friendshipInDb = await _context.Friends.FindAsync(friend.Id);
            Assert.NotNull(friendshipInDb);
            Assert.Equal("accepted", friendshipInDb.Status.ToString().ToLower());
        }

        [Fact]
        public async Task GetFriendsList_ReturnsUsersList()
        {
            // Arrange
            var user1 = new User
            {
                Id = 1,
                Username = "user1",
                Email = "user1@example.com",
                Status = "Active",
                PasswordHash = "hash1"
            };
            var user2 = new User
            {
                Id = 2,
                Username = "user2",
                Email = "user2@example.com",
                Status = "Active",
                PasswordHash = "hash2"
            };
            var user3 = new User
            {
                Id = 3,
                Username = "user3",
                Email = "user3@example.com",
                Status = "Active",
                PasswordHash = "hash3"
            };

            _userServiceMock.Setup(x => x.GetUserByIdAsync(1)).ReturnsAsync(user1);
            _userServiceMock.Setup(x => x.GetUserByIdAsync(2)).ReturnsAsync(user2);
            _userServiceMock.Setup(x => x.GetUserByIdAsync(3)).ReturnsAsync(user3);

            var friends = new List<Friend>
            {
                new Friend
                {
                    UserId = 1,
                    FriendId = 2,
                    Status = FriendStatus.Accepted,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    User = user1,
                    FriendUser = user2
                },
                new Friend
                {
                    UserId = 3,
                    FriendId = 1,
                    Status = FriendStatus.Accepted,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    User = user3,
                    FriendUser = user1
                }
            };
            await _context.Friends.AddRangeAsync(friends);
            await _context.SaveChangesAsync();

            // Act
            var result = await _friendService.GetFriendsList(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Friends.Count);
            Assert.Contains(result.Friends, u => u.Id == 2);
            Assert.Contains(result.Friends, u => u.Id == 3);
        }

        [Fact]
        public async Task RemoveFriend_ValidRequest_RemovesFriendship()
        {
            // Arrange
            var user1 = new User
            {
                Id = 1,
                Username = "user1",
                Email = "user1@example.com",
                Status = "Active",
                PasswordHash = "hash1"
            };
            var user2 = new User
            {
                Id = 2,
                Username = "user2",
                Email = "user2@example.com",
                Status = "Active",
                PasswordHash = "hash2"
            };

            _userServiceMock.Setup(x => x.GetUserByIdAsync(1)).ReturnsAsync(user1);
            _userServiceMock.Setup(x => x.GetUserByIdAsync(2)).ReturnsAsync(user2);

            var friend = new Friend
            {
                UserId = 1,
                FriendId = 2,
                Status = FriendStatus.Accepted,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                User = user1,
                FriendUser = user2
            };
            await _context.Friends.AddAsync(friend);
            await _context.SaveChangesAsync();

            // Act
            var result = await _friendService.RemoveFriend(1, 2);

            // Assert
            Assert.True(result);
            var friendshipInDb = await _context.Friends
                .FirstOrDefaultAsync(f => 
                    (f.UserId == 1 && f.FriendId == 2) ||
                    (f.UserId == 2 && f.FriendId == 1));
            Assert.Null(friendshipInDb);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
    }
} 