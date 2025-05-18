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
using backend.Services.Interfaces;

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
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnValue = Assert.IsType<UserResponseDto>(okResult.Value);
            Assert.Equal(userResponse.Username, returnValue.Username);
            Assert.Equal(userResponse.Email, returnValue.Email);
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
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnValue = Assert.IsType<UserResponseDto>(okResult.Value);
            Assert.Equal(updateDto.Status, returnValue.Status);
            Assert.Equal(updateDto.Bio, returnValue.Bio);
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
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnValue = Assert.IsType<List<UserResponseDto>>(okResult.Value);
            Assert.Equal(2, returnValue.Count);
            Assert.All(returnValue, user => Assert.Contains("testuser", user.Username));
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
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnValue = Assert.IsType<UserResponseDto>(okResult.Value);
            Assert.Equal(userResponse.Username, returnValue.Username);
            Assert.Equal(userResponse.Email, returnValue.Email);
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
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
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
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            var errorObject = new AnonymousObject(badRequestResult.Value!);
            Assert.Equal("Этот email уже зарегистрирован", errorObject.GetPropertyValue<string>("message"));
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
            var fileMock = new Mock<IFormFile>();
            var userResponse = new UserResponseDto
            {
                Id = 1,
                Username = "testuser",
                Email = "test@example.com",
                Status = "Active",
                AvatarUrl = "http://example.com/avatar.jpg"
            };

            _userServiceMock.Setup(x => x.UpdateAvatar(1, fileMock.Object))
                .ReturnsAsync(userResponse);

            // Act
            var result = await _controller.UpdateAvatar(1, fileMock.Object);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnValue = Assert.IsType<UserResponseDto>(okResult.Value);
            Assert.Equal(userResponse.AvatarUrl, returnValue.AvatarUrl);
        }

        [Fact]
        public async Task UpdateAvatar_NonExistingUser_ReturnsNotFound()
        {
            // Arrange
            var fileMock = new Mock<IFormFile>();
            _userServiceMock.Setup(x => x.UpdateAvatar(999, fileMock.Object))
                .ReturnsAsync((UserResponseDto?)null);

            // Act
            var result = await _controller.UpdateAvatar(999, fileMock.Object);

            // Assert
            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task UpdateAvatar_InvalidFile_ReturnsBadRequest()
        {
            // Arrange
            var fileMock = new Mock<IFormFile>();
            _userServiceMock.Setup(x => x.UpdateAvatar(1, fileMock.Object))
                .ThrowsAsync(new Exception("Недопустимый формат файла"));

            // Act
            var result = await _controller.UpdateAvatar(1, fileMock.Object);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            var errorObject = new AnonymousObject(badRequestResult.Value!);
            Assert.Equal("Недопустимый формат файла", errorObject.GetPropertyValue<string>("message"));
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