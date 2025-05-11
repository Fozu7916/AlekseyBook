using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using backend.Controllers;
using backend.Models;
using backend.Models.DTOs;
using backend.Services;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using backend.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace backend.UnitTests
{
    public class UsersControllerTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly Mock<IUserService> _userServiceMock;
        private readonly Mock<ILogger<UsersController>> _loggerMock;
        private readonly UsersController _controller;

        public UsersControllerTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "TestUsersDb")
                .Options;
            _context = new ApplicationDbContext(options);

            _userServiceMock = new Mock<IUserService>();
            _loggerMock = new Mock<ILogger<UsersController>>();

            _controller = new UsersController(_userServiceMock.Object, _loggerMock.Object);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "1"),
                new Claim(ClaimTypes.Name, "testuser")
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };
        }

        [Fact]
        public async Task GetUser_ExistingUser_ReturnsUser()
        {
            // Arrange
            var userResponse = new UserResponseDto
            {
                Id = 1,
                Username = "testuser",
                Email = "test@example.com",
                Status = "Active"
            };

            _userServiceMock.Setup(x => x.GetUserById(1))
                .ReturnsAsync(userResponse);

            // Act
            var result = await _controller.GetUser(1);

            // Assert
            var actionResult = Assert.IsType<ActionResult<UserResponseDto>>(result);
            Assert.Equal(userResponse, actionResult.Value);
            Assert.Equal(userResponse.Username, actionResult.Value?.Username);
            Assert.Equal(userResponse.Email, actionResult.Value?.Email);
        }

        [Fact]
        public async Task UpdateUser_ValidData_ReturnsUpdatedUser()
        {
            // Arrange
            var updateDto = new UpdateUserDto
            {
                Status = "Away",
                Bio = "Test bio"
            };

            var userResponse = new UserResponseDto
            {
                Id = 1,
                Username = "testuser",
                Email = "test@example.com",
                Status = updateDto.Status,
                Bio = updateDto.Bio
            };

            _userServiceMock.Setup(x => x.UpdateUser(1, updateDto))
                .ReturnsAsync(userResponse);

            // Act
            var result = await _controller.UpdateUser(1, updateDto);

            // Assert
            var actionResult = Assert.IsType<ActionResult<UserResponseDto>>(result);
            Assert.Equal(userResponse, actionResult.Value);
            Assert.Equal(updateDto.Status, actionResult.Value?.Status);
            Assert.Equal(updateDto.Bio, actionResult.Value?.Bio);
        }

        [Fact]
        public async Task GetUsers_ReturnsUsersList()
        {
            // Arrange
            var users = new List<UserResponseDto>
            {
                new UserResponseDto { Id = 1, Username = "testuser1", Email = "test1@example.com", Status = "Active" },
                new UserResponseDto { Id = 2, Username = "testuser2", Email = "test2@example.com", Status = "Active" }
            };

            _userServiceMock.Setup(x => x.GetUsers(1, 10))
                .ReturnsAsync(users);

            // Act
            var result = await _controller.GetUsers(1, 10);

            // Assert
            var actionResult = Assert.IsType<ActionResult<List<UserResponseDto>>>(result);
            Assert.Equal(users, actionResult.Value);
            Assert.Equal(2, actionResult.Value?.Count);
            Assert.All(actionResult.Value!, user => Assert.Contains("testuser", user.Username));
        }

        [Fact]
        public async Task GetUser_NonExistingUser_ReturnsNotFound()
        {
            // Arrange
            _userServiceMock.Setup(x => x.GetUserById(999))
                .ReturnsAsync((UserResponseDto?)null);

            // Act
            var result = await _controller.GetUser(999);

            // Assert
            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task GetUserByUsername_ExistingUser_ReturnsUser()
        {
            // Arrange
            var userResponse = new UserResponseDto
            {
                Id = 1,
                Username = "testuser",
                Email = "test@example.com",
                Status = "Active"
            };

            _userServiceMock.Setup(x => x.GetUserByUsername("testuser"))
                .ReturnsAsync(userResponse);

            // Act
            var result = await _controller.GetUserByUsername("testuser");

            // Assert
            var actionResult = Assert.IsType<ActionResult<UserResponseDto>>(result);
            Assert.Equal(userResponse, actionResult.Value);
            Assert.Equal(userResponse.Username, actionResult.Value?.Username);
            Assert.Equal(userResponse.Email, actionResult.Value?.Email);
        }

        [Fact]
        public async Task GetUserByUsername_NonExistingUser_ReturnsNotFound()
        {
            // Arrange
            _userServiceMock.Setup(x => x.GetUserByUsername("nonexistent"))
                .ReturnsAsync((UserResponseDto?)null);

            // Act
            var result = await _controller.GetUserByUsername("nonexistent");

            // Assert
            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task CreateUser_ValidData_ReturnsOkResult()
        {
            // Arrange
            var registerDto = new RegisterUserDto
            {
                Username = "newuser",
                Email = "newuser@example.com",
                Password = "Password123!"
            };

            var authResponse = new AuthResponseDto
            {
                Token = "test-token",
                User = new UserResponseDto
                {
                    Id = 1,
                    Username = registerDto.Username,
                    Email = registerDto.Email,
                    Status = "Active"
                }
            };

            _userServiceMock.Setup(x => x.Register(registerDto))
                .ReturnsAsync(authResponse);

            // Act
            var result = await _controller.CreateUser(registerDto);

            // Assert
            var actionResult = Assert.IsType<ActionResult<AuthResponseDto>>(result);
            Assert.NotNull(actionResult.Result);
            var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
            Assert.NotNull(okResult.Value);
            var returnValue = Assert.IsType<AuthResponseDto>(okResult.Value);
            Assert.NotNull(returnValue.User);
            Assert.NotNull(returnValue.Token);
            Assert.Equal(registerDto.Username, returnValue.User.Username);
            Assert.Equal(registerDto.Email, returnValue.User.Email);
        }

        [Fact]
        public async Task CreateUser_DuplicateEmail_ReturnsBadRequest()
        {
            // Arrange
            var registerDto = new RegisterUserDto
            {
                Username = "newuser",
                Email = "existing@example.com",
                Password = "Password123!"
            };

            _userServiceMock.Setup(x => x.Register(registerDto))
                .ThrowsAsync(new Exception("Этот email уже зарегистрирован"));

            // Act
            var result = await _controller.CreateUser(registerDto);

            // Assert
            var actionResult = Assert.IsType<ActionResult<AuthResponseDto>>(result);
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(actionResult.Result);
            var errorMessage = Assert.IsType<JsonResult>(new JsonResult(badRequestResult.Value)).Value
                .GetType()
                .GetProperty("message")
                .GetValue(badRequestResult.Value)
                .ToString();
            Assert.Equal("Этот email уже зарегистрирован", errorMessage);
        }

        [Fact]
        public async Task DeleteUser_ExistingUser_ReturnsNoContent()
        {
            // Arrange
            _userServiceMock.Setup(x => x.DeleteUser(1))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.DeleteUser(1);

            // Assert
            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task DeleteUser_NonExistingUser_ReturnsNotFound()
        {
            // Arrange
            _userServiceMock.Setup(x => x.DeleteUser(999))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.DeleteUser(999);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task UpdateAvatar_ValidFile_ReturnsUpdatedUser()
        {
            // Arrange
            var formFile = new Mock<IFormFile>();
            formFile.Setup(f => f.Length).Returns(1024); // 1KB
            formFile.Setup(f => f.ContentType).Returns("image/jpeg");

            var userResponse = new UserResponseDto
            {
                Id = 1,
                Username = "testuser",
                Email = "test@example.com",
                Status = "Active",
                AvatarUrl = "/uploads/avatars/testuser.jpg"
            };

            _userServiceMock.Setup(x => x.UpdateAvatar(1, formFile.Object))
                .ReturnsAsync(userResponse);

            // Act
            var result = await _controller.UpdateAvatar(1, formFile.Object);

            // Assert
            var actionResult = Assert.IsType<ActionResult<UserResponseDto>>(result);
            Assert.NotNull(actionResult.Value);
            var returnValue = actionResult.Value;
            Assert.NotNull(returnValue.AvatarUrl);
            Assert.Equal(userResponse.AvatarUrl, returnValue.AvatarUrl);
        }

        [Fact]
        public async Task UpdateAvatar_NonExistingUser_ReturnsNotFound()
        {
            // Arrange
            var formFile = new Mock<IFormFile>();
            formFile.Setup(f => f.Length).Returns(1024); // 1KB
            formFile.Setup(f => f.ContentType).Returns("image/jpeg");

            _userServiceMock.Setup(x => x.UpdateAvatar(999, formFile.Object))
                .ReturnsAsync((UserResponseDto?)null);

            // Act
            var result = await _controller.UpdateAvatar(999, formFile.Object);

            // Assert
            var actionResult = Assert.IsType<ActionResult<UserResponseDto>>(result);
            Assert.IsType<NotFoundResult>(actionResult.Result);
        }

        [Fact]
        public async Task UpdateAvatar_InvalidFile_ReturnsBadRequest()
        {
            // Arrange
            var formFile = new Mock<IFormFile>();
            formFile.Setup(f => f.Length).Returns(1024 * 1024 * 11); // 11MB
            formFile.Setup(f => f.ContentType).Returns("image/jpeg");

            _userServiceMock.Setup(x => x.UpdateAvatar(1, formFile.Object))
                .ThrowsAsync(new Exception("Размер файла превышает 10MB"));

            // Act
            var result = await _controller.UpdateAvatar(1, formFile.Object);

            // Assert
            var actionResult = Assert.IsType<ActionResult<UserResponseDto>>(result);
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(actionResult.Result);
            var errorMessage = Assert.IsType<JsonResult>(new JsonResult(badRequestResult.Value)).Value
                .GetType()
                .GetProperty("message")
                .GetValue(badRequestResult.Value)
                .ToString();
            Assert.Equal("Размер файла превышает 10MB", errorMessage);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
    }

    // Вспомогательный класс для работы с анонимными объектами
    public class AnonymousObject
    {
        private readonly object _value;

        public AnonymousObject(object value)
        {
            _value = value ?? throw new ArgumentNullException(nameof(value));
        }

        public T GetPropertyValue<T>(string propertyName)
        {
            if (_value == null)
                throw new ArgumentNullException(nameof(_value));

            var property = _value.GetType().GetProperty(propertyName) 
                ?? throw new ArgumentException($"Property {propertyName} not found", nameof(propertyName));
                
            var value = property.GetValue(_value);
            if (value == null)
                throw new InvalidOperationException($"Property {propertyName} value is null");

            return (T)value;
        }
    }
} 