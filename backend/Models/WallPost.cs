using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models
{
    public class WallPost
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Content { get; set; }

        public string? ImageUrl { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public int AuthorId { get; set; }

        [ForeignKey("AuthorId")]
        public User Author { get; set; }

        [Required]
        public int WallOwnerId { get; set; }

        [ForeignKey("WallOwnerId")]
        public User WallOwner { get; set; }

        public bool IsDeleted { get; set; }

        public ICollection<Like> Likes { get; set; }
        public ICollection<Comment> Comments { get; set; }
    }
} 