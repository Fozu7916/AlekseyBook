using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models
{
    public class Community
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Name { get; set; }
        
        public string Description { get; set; }
        
        public string AvatarUrl { get; set; }
        
        [Required]
        public int CreatorId { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("CreatorId")]
        public User Creator { get; set; }
        
        public ICollection<CommunityMember> Members { get; set; }
        public ICollection<CommunityPost> Posts { get; set; }
    }

    public class CommunityMember
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public int CommunityId { get; set; }
        
        [Required]
        public int UserId { get; set; }
        
        [Required]
        [StringLength(20)]
        public string Role { get; set; } = "member"; // admin, moderator, member
        
        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("CommunityId")]
        public Community Community { get; set; }
        
        [ForeignKey("UserId")]
        public User User { get; set; }
    }

    public class CommunityPost
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public int CommunityId { get; set; }
        
        [Required]
        public int AuthorId { get; set; }
        
        [Required]
        public string Content { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("CommunityId")]
        public Community Community { get; set; }
        
        [ForeignKey("AuthorId")]
        public User Author { get; set; }
    }
} 