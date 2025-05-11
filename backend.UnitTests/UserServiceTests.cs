using Xunit;
using Moq;
using backend.Services;
using backend.Models;
using backend.Models.DTOs;
using backend.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Hosting;

namespace backend.UnitTests
{
    public class UserServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly Mock<IConfiguration> _configurationMock;
        private readonly Mock<IWebHostEnvironment> _environmentMock;
        private readonly Mock<ILogger<UserService>> _loggerMock;
        private readonly UserService _userService;

        public UserServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "TestUserServiceDb")
                .Options;
            _context = new ApplicationDbContext(options);

            _configurationMock = new Mock<IConfiguration>();
            _environmentMock = new Mock<IWebHostEnvironment>();
            _loggerMock = new Mock<ILogger<UserService>>();

            // Настраиваем конфигурацию JWT
            _configurationMock.Setup(x => x["Jwt:Key"]).Returns("your-test-secret-key-min-16-chars");
            _configurationMock.Setup(x => x["Jwt:Issuer"]).Returns("test-issuer");
            _configurationMock.Setup(x => x["Jwt:Audience"]).Returns("test-audience");

            _userService = new UserService(
                _context,
                _environmentMock.Object,
                _configurationMock.Object,
                _loggerMock.Object
            );
        }

        [Fact]
        public async Task Register_ValidUser_ReturnsAuthResponse()
        {
            // Arrange
            var registerDto = new RegisterUserDto
            {
                Username = "testuser",
                Email = "test@example.com",
                Password = "Test123!"
            };

            // Act
            var result = await _userService.Register(registerDto);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Token);
            Assert.NotNull(result.User);
            Assert.Equal(registerDto.Username, result.User.Username);
            Assert.Equal(registerDto.Email, result.User.Email);

            var userInDb = await _context.Users.FirstOrDefaultAsync(u => u.Email == registerDto.Email);
            Assert.NotNull(userInDb);
            Assert.Equal(registerDto.Username, userInDb.Username);
        }

        [Fact]
        public async Task Register_DuplicateEmail_ThrowsException()
        {
            // Arrange
            var existingUser = new User
            {
                Username = "existinguser",
                Email = "existing@example.com",
                PasswordHash = "hashedpassword",
                CreatedAt = DateTime.UtcNow,
                LastLogin = DateTime.UtcNow,
                Status = "Active"
            };
            await _context.Users.AddAsync(existingUser);
            await _context.SaveChangesAsync();

            var registerDto = new RegisterUserDto
            {
                Username = "newuser",
                Email = "existing@example.com",
                Password = "Test123!"
            };

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => _userService.Register(registerDto));
        }

        [Fact]
        public async Task Login_ValidCredentials_ReturnsAuthResponse()
        {
            // Arrange
            var password = "Test123!";
            var hashedPassword = _userService.HashPassword(password);
            var user = new User
            {
                Username = "testuser",
                Email = "test@example.com",
                PasswordHash = hashedPassword,
                CreatedAt = DateTime.UtcNow,
                LastLogin = DateTime.UtcNow,
                Status = "Active"
            };
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            var loginDto = new LoginUserDto
            {
                Email = "test@example.com",
                Password = password
            };

            // Act
            var result = await _userService.Login(loginDto);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Token);
            Assert.NotNull(result.User);
            Assert.Equal(user.Username, result.User.Username);
            Assert.Equal(user.Email, result.User.Email);
        }

        [Fact]
        public async Task Login_InvalidCredentials_ReturnsNull()
        {
            // Arrange
            var user = new User
            {
                Username = "testuser",
                Email = "test@example.com",
                PasswordHash = _userService.HashPassword("correctpassword"),
                CreatedAt = DateTime.UtcNow,
                LastLogin = DateTime.UtcNow,
                Status = "Active"
            };
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            var loginDto = new LoginUserDto
            {
                Email = "test@example.com",
                Password = "wrongpassword"
            };

            // Act
            var result = await _userService.Login(loginDto);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetUserById_ExistingUser_ReturnsUser()
        {
            // Arrange
            var user = new User
            {
                Username = "testuser",
                Email = "test@example.com",
                PasswordHash = "hashedpassword",
                CreatedAt = DateTime.UtcNow,
                LastLogin = DateTime.UtcNow,
                Status = "Active"
            };
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            // Act
            var result = await _userService.GetUserById(user.Id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(user.Username, result.Username);
            Assert.Equal(user.Email, result.Email);
        }

        [Fact]
        public async Task GetUserById_NonExistingUser_ReturnsNull()
        {
            // Act
            var result = await _userService.GetUserById(999);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task UpdateUser_ValidData_ReturnsUpdatedUser()
        {
            // Arrange
            var user = new User
            {
                Username = "testuser",
                Email = "test@example.com",
                PasswordHash = "hashedpassword",
                CreatedAt = DateTime.UtcNow,
                LastLogin = DateTime.UtcNow,
                Status = "Active"
            };
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            var updateDto = new UpdateUserDto
            {
                Status = "Away",
                Bio = "Test bio"
            };

            // Act
            var result = await _userService.UpdateUser(user.Id, updateDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(updateDto.Status, result.Status);
            Assert.Equal(updateDto.Bio, result.Bio);

            var userInDb = await _context.Users.FindAsync(user.Id);
            Assert.NotNull(userInDb);
            Assert.Equal(updateDto.Status, userInDb.Status);
            Assert.Equal(updateDto.Bio, userInDb.Bio);
        }

        [Fact]
        public void HashPassword_ValidPassword_ReturnsHash()
        {
            // Arrange
            var password = "Test123!";

            // Act
            var hash = _userService.HashPassword(password);

            // Assert
            Assert.NotNull(hash);
            Assert.NotEqual(password, hash);
            Assert.True(_userService.VerifyPassword(password, hash));
        }

        [Fact]
        public void VerifyPassword_ValidPassword_ReturnsTrue()
        {
            // Arrange
            var password = "Test123!";
            var hash = _userService.HashPassword(password);

            // Act
            var result = _userService.VerifyPassword(password, hash);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void VerifyPassword_InvalidPassword_ReturnsFalse()
        {
            // Arrange
            var password = "Test123!";
            var hash = _userService.HashPassword(password);

            // Act
            var result = _userService.VerifyPassword("wrongpassword", hash);

            // Assert
            Assert.False(result);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
    }
} 