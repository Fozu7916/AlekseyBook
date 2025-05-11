using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using backend.Controllers;
using backend.Data;
using backend.Models;
using backend.Models.DTOs;
using backend.Services;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace backend.UnitTests
{
    public class AuthControllerTests : IDisposable
    {
        private readonly Mock<IConfiguration> _configurationMock;
        private readonly Mock<IUserService> _userServiceMock;
        private readonly ApplicationDbContext _context;
        private readonly Mock<ILogger<AuthController>> _loggerMock;
        private readonly AuthController _controller;

        public AuthControllerTests()
        {
            // Настраиваем in-memory базу данных для тестов
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "TestAuthDb")
                .Options;
            _context = new ApplicationDbContext(options);

            _configurationMock = new Mock<IConfiguration>();
            _userServiceMock = new Mock<IUserService>();
            _loggerMock = new Mock<ILogger<AuthController>>();

            // Настраиваем конфигурацию JWT
            _configurationMock.Setup(x => x["Jwt:Key"]).Returns("your-test-secret-key-min-16-chars");
            _configurationMock.Setup(x => x["Jwt:Issuer"]).Returns("test-issuer");
            _configurationMock.Setup(x => x["Jwt:Audience"]).Returns("test-audience");

            _controller = new AuthController(
                _context,
                _configurationMock.Object,
                _userServiceMock.Object,
                _loggerMock.Object
            );

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
        public async Task Register_ValidUser_ReturnsOkResult()
        {
            // Arrange
            var registerDto = new RegisterUserDto
            {
                Username = "testuser",
                Email = "test@example.com",
                Password = "Test123!"
            };

            _userServiceMock.Setup(x => x.HashPassword(registerDto.Password))
                .Returns("hashedPassword");

            // Act
            var result = await _controller.Register(registerDto);

            // Assert
            var okResult = Assert.IsType<ActionResult<AuthResponseDto>>(result);
            var value = Assert.IsType<OkObjectResult>(okResult.Result);
            var authResponse = Assert.IsType<AuthResponseDto>(value.Value);
            Assert.NotNull(authResponse.Token);
            Assert.Equal(registerDto.Username, authResponse.User.Username);
            Assert.Equal(registerDto.Email, authResponse.User.Email);
        }

        [Fact]
        public async Task Login_ValidCredentials_ReturnsOkResult()
        {
            // Arrange
            var loginDto = new LoginUserDto
            {
                Email = "test@example.com",
                Password = "Test123!"
            };

            var user = new User
            {
                Email = loginDto.Email,
                Username = "testuser",
                PasswordHash = "hashedPassword",
                CreatedAt = DateTime.UtcNow,
                LastLogin = DateTime.UtcNow,
                Status = "Active",
                IsVerified = true
            };

            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            _userServiceMock.Setup(x => x.VerifyPassword(loginDto.Password, user.PasswordHash))
                .Returns(true);

            // Act
            var result = await _controller.Login(loginDto);

            // Assert
            var okResult = Assert.IsType<ActionResult<UserResponseDto>>(result);
            var value = Assert.IsType<OkObjectResult>(okResult.Result);
            var authResponse = Assert.IsType<AuthResponseDto>(value.Value);
            Assert.NotNull(authResponse.Token);
            Assert.Equal(user.Email, authResponse.User.Email);
        }

        [Fact]
        public async Task Register_ExistingEmail_ReturnsBadRequest()
        {
            // Arrange
            var existingUser = new User
            {
                Email = "existing@example.com",
                Username = "existinguser",
                PasswordHash = "hashedPassword",
                CreatedAt = DateTime.UtcNow,
                LastLogin = DateTime.UtcNow,
                Status = "Active",
                IsVerified = true
            };

            await _context.Users.AddAsync(existingUser);
            await _context.SaveChangesAsync();

            var registerDto = new RegisterUserDto
            {
                Username = "newuser",
                Email = "existing@example.com",
                Password = "Test123!"
            };

            // Act
            var result = await _controller.Register(registerDto);

            // Assert
            var badRequestResult = Assert.IsType<ActionResult<AuthResponseDto>>(result);
            Assert.IsType<BadRequestObjectResult>(badRequestResult.Result);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
    }
} 