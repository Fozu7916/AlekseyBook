using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using backend.Controllers;
using backend.Services.Interfaces;
using backend.Models.DTOs;
using System.Security.Claims;
using Xunit;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System;
using Newtonsoft.Json.Linq;

namespace backend.tests.Controllers
{
    public class NotificationsControllerTests
    {
        private readonly Mock<INotificationService> _mockNotificationService;
        private readonly Mock<ILogger<NotificationsController>> _mockLogger;
        private readonly NotificationsController _controller;

        public NotificationsControllerTests()
        {
            _mockNotificationService = new Mock<INotificationService>();
            _mockLogger = new Mock<ILogger<NotificationsController>>();
            _controller = new NotificationsController(_mockNotificationService.Object, _mockLogger.Object);

            // Настройка Claims для авторизованного пользователя
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "1")
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };
        }

        [Fact]
        public async Task GetNotifications_ReturnsOkResult_WithNotifications()
        {
            // Arrange
            var expectedNotifications = new List<NotificationDto>
            {
                new NotificationDto 
                { 
                    Id = 1, 
                    Type = "Message",
                    Title = "Test Title",
                    Text = "Test Text",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                }
            };
            _mockNotificationService.Setup(x => x.GetUserNotifications(1))
                .ReturnsAsync(expectedNotifications);

            // Act
            var result = await _controller.GetNotifications();

            // Assert
            var okResult = Assert.IsType<ActionResult<List<NotificationDto>>>(result);
            var returnValue = Assert.IsType<OkObjectResult>(okResult.Result);
            var notifications = Assert.IsType<List<NotificationDto>>(returnValue.Value);
            Assert.Single(notifications);
            Assert.Equal(expectedNotifications[0].Id, notifications[0].Id);
            Assert.Equal(expectedNotifications[0].Title, notifications[0].Title);
            Assert.Equal(expectedNotifications[0].Text, notifications[0].Text);
        }

        [Fact]
        public async Task GetUnreadCount_ReturnsOkResult_WithCount()
        {
            // Arrange
            const int expectedCount = 5;
            _mockNotificationService.Setup(x => x.GetUnreadNotificationsCount(1))
                .ReturnsAsync(expectedCount);

            // Act
            var result = await _controller.GetUnreadCount();

            // Assert
            var okResult = Assert.IsType<ActionResult<int>>(result);
            var returnValue = Assert.IsType<OkObjectResult>(okResult.Result);
            var count = Assert.IsType<int>(returnValue.Value);
            Assert.Equal(expectedCount, count);
        }

        [Fact]
        public async Task MarkAsRead_ReturnsOkResult_WhenSuccessful()
        {
            // Arrange
            const int notificationId = 1;
            _mockNotificationService.Setup(x => x.MarkAsRead(1, notificationId))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.MarkAsRead(notificationId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = JObject.FromObject(okResult.Value);
            Assert.True(response["success"].Value<bool>());
        }

        [Fact]
        public async Task MarkAllAsRead_ReturnsOkResult_WhenSuccessful()
        {
            // Arrange
            _mockNotificationService.Setup(x => x.MarkAllAsRead(1))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.MarkAllAsRead();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = JObject.FromObject(okResult.Value);
            Assert.True(response["success"].Value<bool>());
        }

        [Fact]
        public async Task DeleteNotification_ReturnsOkResult_WhenSuccessful()
        {
            // Arrange
            const int notificationId = 1;
            _mockNotificationService.Setup(x => x.DeleteNotification(1, notificationId))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.DeleteNotification(notificationId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = JObject.FromObject(okResult.Value);
            Assert.True(response["success"].Value<bool>());
        }

        [Fact]
        public async Task CreateNotification_ReturnsOkResult_WithCreatedNotification()
        {
            // Arrange
            var notificationDto = new CreateNotificationDto
            {
                UserId = 1,
                Type = "Message",
                Title = "Test Title",
                Text = "Test Text",
                Link = "/test"
            };
            var expectedNotification = new NotificationDto
            {
                Id = 1,
                Type = "Message",
                Title = "Test Title",
                Text = "Test Text",
                Link = "/test",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };
            _mockNotificationService.Setup(x => x.CreateNotification(notificationDto))
                .ReturnsAsync(expectedNotification);

            // Act
            var result = await _controller.CreateNotification(notificationDto);

            // Assert
            var okResult = Assert.IsType<ActionResult<NotificationDto>>(result);
            var returnValue = Assert.IsType<OkObjectResult>(okResult.Result);
            var notification = Assert.IsType<NotificationDto>(returnValue.Value);
            Assert.Equal(expectedNotification.Id, notification.Id);
            Assert.Equal(expectedNotification.Title, notification.Title);
            Assert.Equal(expectedNotification.Text, notification.Text);
        }

        [Fact]
        public async Task GetNotifications_ReturnsBadRequest_WhenExceptionOccurs()
        {
            // Arrange
            _mockNotificationService.Setup(x => x.GetUserNotifications(1))
                .ThrowsAsync(new Exception("Test error"));

            // Act
            var result = await _controller.GetNotifications();

            // Assert
            var badRequestResult = Assert.IsType<ActionResult<List<NotificationDto>>>(result);
            var returnValue = Assert.IsType<BadRequestObjectResult>(badRequestResult.Result);
            var response = JObject.FromObject(returnValue.Value);
            Assert.Equal("Test error", response["message"].Value<string>());
        }
    }
} 