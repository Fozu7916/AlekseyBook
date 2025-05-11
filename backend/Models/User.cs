using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models
{
    [Table("users")]
    public class User
    {
        public User()
        {
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
            IsOnline = false;
            IsVerified = false;
            IsBanned = false;
        }

        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public required string Username { get; set; }

        [Required]
        [EmailAddress]
        [StringLength(100)]
        public required string Email { get; set; }

        [Required]
        public required string PasswordHash { get; set; }

        public string? AvatarUrl { get; set; }

        [StringLength(100)]
        public string? Status { get; set; }

        public DateTime CreatedAt { get; set; }
        
        public DateTime UpdatedAt { get; set; }
        
        public DateTime? LastLogin { get; set; }
        
        public bool IsVerified { get; set; }
        
        public bool IsBanned { get; set; }

        public bool IsOnline { get; set; }
        
        public string? Bio { get; set; }
    }
} 