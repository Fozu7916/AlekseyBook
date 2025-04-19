using System;

namespace backend.Models.DTOs
{
    public class UserResponseDto
    {
        public int Id { get; set; }
        public required string Username { get; set; }
        public required string Email { get; set; }
        public string? AvatarUrl { get; set; }
        public string? Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLogin { get; set; }
        public bool IsVerified { get; set; }
        public string? Bio { get; set; }
    }

    public class CreateUserDto
    {
        public required string Username { get; set; }
        public required string Email { get; set; }
        public required string Password { get; set; }
        public string? AvatarUrl { get; set; }
        public string? Bio { get; set; }
    }

    public class UpdateUserDto
    {
        public string? Status { get; set; }
        public string? Bio { get; set; }
    }

    public class LoginDto
    {
        public required string Email { get; set; }
        public required string Password { get; set; }
    }

    public class AuthResponseDto
    {
        public required string Token { get; set; }
        public required UserResponseDto User { get; set; }
    }
} 