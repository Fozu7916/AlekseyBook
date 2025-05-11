using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models
{
    public class Community
    {
        public Community()
        {
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
            Members = new List<CommunityMember>();
            Posts = new List<CommunityPost>();
        }

        [Key]
        public int Id { get; set; }
        
        [Required]
        [StringLength(100)]
        public required string Name { get; set; }
        
        public required string Description { get; set; }
        
        public required string AvatarUrl { get; set; }
        
        [Required]
        public int CreatorId { get; set; }
        
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        [ForeignKey("CreatorId")]
        public required User Creator { get; set; }
        
        public required ICollection<CommunityMember> Members { get; set; }
        public required ICollection<CommunityPost> Posts { get; set; }
    }

    public class CommunityMember
    {
        public CommunityMember()
        {
            Role = "member";
            JoinedAt = DateTime.UtcNow;
        }

        [Key]
        public int Id { get; set; }
        
        [Required]
        public int CommunityId { get; set; }
        
        [Required]
        public int UserId { get; set; }
        
        [Required]
        [StringLength(20)]
        public required string Role { get; set; } // admin, moderator, member
        
        public DateTime JoinedAt { get; set; }

        [ForeignKey("CommunityId")]
        public required Community Community { get; set; }
        
        [ForeignKey("UserId")]
        public required User User { get; set; }
    }

    public class CommunityPost
    {
        public CommunityPost()
        {
            CreatedAt = DateTime.UtcNow;
        }

        [Key]
        public int Id { get; set; }
        
        [Required]
        public int CommunityId { get; set; }
        
        [Required]
        public int AuthorId { get; set; }
        
        [Required]
        public required string Content { get; set; }
        
        public DateTime CreatedAt { get; set; }

        [ForeignKey("CommunityId")]
        public required Community Community { get; set; }
        
        [ForeignKey("AuthorId")]
        public required User Author { get; set; }
    }
} 