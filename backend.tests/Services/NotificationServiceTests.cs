using Xunit;
using Moq;
using backend.Services;
using backend.Models;
using backend.Models.DTOs;
using backend.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using backend.Hubs;
using backend.Hubs.Interfaces;

namespace backend.UnitTests.Services
{
    public class NotificationServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly Mock<IHubContext<NotificationHub, INotificationClient>> _mockHubContext;
        private readonly Mock<IHubClients<INotificationClient>> _mockHubClients;
        private readonly Mock<INotificationClient> _mockClientProxy;
        private readonly NotificationService _notificationService;

        public NotificationServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            
            _mockClientProxy = new Mock<INotificationClient>();
            _mockHubClients = new Mock<IHubClients<INotificationClient>>();
            _mockHubContext = new Mock<IHubContext<NotificationHub, INotificationClient>>();
            
            _mockHubClients
                .Setup(clients => clients.Group(It.IsAny<string>()))
                .Returns(_mockClientProxy.Object);
            
            _mockHubContext
                .Setup(x => x.Clients)
                .Returns(_mockHubClients.Object);

            _notificationService = new NotificationService(_context, _mockHubContext.Object);
        }

        [Fact]
        public async Task CreateNotification_ValidData_CreatesAndNotifies()
        {
            // Arrange
            var user = new User 
            { 
                Id = 1, 
                Username = "testuser",
                Email = "test@test.com",
                PasswordHash = "hash"
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var createDto = new CreateNotificationDto
            {
                UserId = user.Id,
                Type = "Message",
                Title = "Test Title",
                Text = "Test Message",
                Link = "/test"
            };

            // Act
            var result = await _notificationService.CreateNotification(createDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(createDto.Title, result.Title);
            Assert.Equal(createDto.Text, result.Text);
            Assert.Equal(createDto.Link, result.Link);
            Assert.False(result.IsRead);

            var dbNotification = await _context.Notifications.FirstOrDefaultAsync();
            Assert.NotNull(dbNotification);
            Assert.Equal(createDto.Title, dbNotification.Title);

            _mockClientProxy.Verify(
                x => x.ReceiveNotification(
                    It.Is<NotificationDto>(n => n.Id == result.Id)),
                Times.Once);
        }

        [Fact]
        public async Task GetUserNotifications_ReturnsUserNotifications()
        {
            // Arrange
            var user = new User 
            { 
                Id = 1, 
                Username = "testuser",
                Email = "test@test.com",
                PasswordHash = "hash"
            };
            _context.Users.Add(user);

            var notifications = new List<Notification>
            {
                new Notification 
                { 
                    UserId = user.Id,
                    Type = NotificationType.Message,
                    Title = "Test 1",
                    Text = "Message 1",
                    CreatedAt = DateTime.UtcNow,
                    User = user
                },
                new Notification 
                { 
                    UserId = user.Id,
                    Type = NotificationType.Friend,
                    Title = "Test 2",
                    Text = "Message 2",
                    CreatedAt = DateTime.UtcNow,
                    User = user
                }
            };
            _context.Notifications.AddRange(notifications);
            await _context.SaveChangesAsync();

            // Act
            var result = await _notificationService.GetUserNotifications(user.Id);

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Contains(result, n => n.Title == "Test 1");
            Assert.Contains(result, n => n.Title == "Test 2");
        }

        [Fact]
        public async Task MarkAsRead_ValidNotification_MarksAsRead()
        {
            // Arrange
            var user = new User 
            { 
                Id = 1, 
                Username = "testuser",
                Email = "test@test.com",
                PasswordHash = "hash"
            };
            _context.Users.Add(user);

            var notification = new Notification
            {
                UserId = user.Id,
                Type = NotificationType.Message,
                Title = "Test",
                Text = "Message",
                IsRead = false,
                CreatedAt = DateTime.UtcNow,
                User = user
            };
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            // Act
            await _notificationService.MarkAsRead(user.Id, notification.Id);

            // Assert
            var dbNotification = await _context.Notifications.FindAsync(notification.Id);
            Assert.True(dbNotification?.IsRead);
            
            _mockClientProxy.Verify(
                x => x.NotificationRead(notification.Id),
                Times.Once);
        }

        [Fact]
        public async Task DeleteNotification_ValidNotification_Deletes()
        {
            // Arrange
            var user = new User 
            { 
                Id = 1, 
                Username = "testuser",
                Email = "test@test.com",
                PasswordHash = "hash"
            };
            _context.Users.Add(user);

            var notification = new Notification
            {
                UserId = user.Id,
                Type = NotificationType.Message,
                Title = "Test",
                Text = "Message",
                CreatedAt = DateTime.UtcNow,
                User = user
            };
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            // Act
            await _notificationService.DeleteNotification(user.Id, notification.Id);

            // Assert
            var dbNotification = await _context.Notifications.FindAsync(notification.Id);
            Assert.Null(dbNotification);
            
            _mockClientProxy.Verify(
                x => x.NotificationDeleted(notification.Id),
                Times.Once);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
    }
} 