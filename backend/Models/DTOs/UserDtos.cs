using System.ComponentModel.DataAnnotations;

namespace Backend.Models.DTOs
{
    public class CreateUserDto
    {
        [Required]
        [StringLength(255)]
        public required string Username { get; set; }

        [Required]
        [EmailAddress]
        [StringLength(255)]
        public required string Email { get; set; }
        
        [Required]
        [StringLength(255, MinimumLength = 6)]
        public required string Password { get; set; }

        public string? AvatarUrl { get; set; }
        public string? Bio { get; set; }
    }

    public class UpdateUserDto
    {
        [Required(ErrorMessage = "Статус обязателен для заполнения")]
        [StringLength(50, ErrorMessage = "Статус не может быть длиннее 50 символов")]
        public string Status { get; set; } = string.Empty;

        [StringLength(1000, ErrorMessage = "Биография не может быть длиннее 1000 символов")]
        public string? Bio { get; set; }
    }

    public class UserResponseDto
    {
        public int Id { get; set; }
        public required string Username { get; set; }
        public required string Email { get; set; }
        public string? AvatarUrl { get; set; }
        public required string Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLogin { get; set; }
        public bool IsVerified { get; set; }
        public string? Bio { get; set; }
    }

    public class LoginDto
    {
        [Required]
        [EmailAddress]
        public required string Email { get; set; }

        [Required]
        public required string Password { get; set; }
    }

    public class AuthResponseDto
    {
        public required string Token { get; set; }
        public required UserResponseDto User { get; set; }
    }
} 