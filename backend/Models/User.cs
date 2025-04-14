using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Models
{
    [Table("users")]
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(255)]
        public required string Username { get; set; }

        [Required]
        [EmailAddress]
        [StringLength(255)]
        public required string Email { get; set; }

        [Required]
        [StringLength(255)]
        [Column("password_hash")]
        public required string PasswordHash { get; set; }

        [StringLength(255)]
        [Column("avatar_url")]
        public string? AvatarUrl { get; set; }

        [StringLength(50)]
        [Column("status", TypeName = "varchar(50)")]
        public string Status { get; set; } = "online";

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [Column("last_login")]
        public DateTime? LastLogin { get; set; }

        [Column("is_verified")]
        public bool IsVerified { get; set; } = false;

        [Column("is_banned")]
        public bool IsBanned { get; set; } = false;

        public string? Bio { get; set; }
    }
} 