using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models
{
    [Table("users")]
    public class User
    {
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

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? LastLogin { get; set; }
        
        public bool IsVerified { get; set; }
        
        public bool IsBanned { get; set; }

        public bool IsOnline { get; set; } = false;
        
        public string? Bio { get; set; }

        // Навигационные свойства
        public virtual ICollection<Friend> FriendRequestsSent { get; set; } = new List<Friend>();
        public virtual ICollection<Friend> FriendRequestsReceived { get; set; } = new List<Friend>();
        public virtual ICollection<Message> MessagesSent { get; set; } = new List<Message>();
        public virtual ICollection<Message> MessagesReceived { get; set; } = new List<Message>();
        public virtual ICollection<UserTrack> UserTracks { get; set; } = new List<UserTrack>();
        public virtual ICollection<CommunityMember> Communities { get; set; } = new List<CommunityMember>();
        public virtual ICollection<Post> Posts { get; set; } = new List<Post>();
    }
} 