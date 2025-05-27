using Microsoft.AspNetCore.Http;
using backend.Models;
using backend.Models.DTOs;

namespace backend.Services.Interfaces
{
    public interface IUserService
    {
        Task<AuthResponseDto> Register(RegisterUserDto registerUserDto);
        Task<AuthResponseDto?> Login(LoginUserDto loginDto);
        Task<UserResponseDto?> GetUserById(int id);
        Task<UserResponseDto?> GetUserByUsername(string username);
        Task<List<UserResponseDto>> GetUsers(int page = 1, int pageSize = 10);
        Task<List<UserResponseDto>> SearchUsers(string searchTerm, int page = 1, int pageSize = 10);
        Task<UserResponseDto?> UpdateUser(int id, UpdateUserDto updateUserDto);
        Task<bool> DeleteUser(int id);
        string HashPassword(string password);
        bool VerifyPassword(string password, string hashedPassword);
        Task<UserResponseDto?> UpdateAvatar(int userId, IFormFile avatar);
        Task<User> GetUserByIdAsync(int id);
        Task<User> GetUserByUsernameAsync(string username);
        Task<User> GetUserByEmailAsync(string email);
        Task<bool> IsEmailTakenAsync(string email);
        Task<bool> IsUsernameTakenAsync(string username);
        Task<(UserResponseDto User, string Token)> RegisterUserAsync(RegisterUserDto registerData);
        Task<(UserResponseDto User, string Token)> LoginUserAsync(LoginUserDto loginData);
        Task<string> UpdateUserAvatarAsync(int id, string avatarUrl);
    }
} 