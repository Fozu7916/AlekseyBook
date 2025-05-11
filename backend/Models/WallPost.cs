using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models
{
    public class WallPost
    {
        public WallPost()
        {
            Likes = new List<Like>();
            Comments = new List<Comment>();
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        [Key]
        public int Id { get; set; }

        [Required]
        public required string Content { get; set; }

        public string? ImageUrl { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; }

        [Required]
        public DateTime UpdatedAt { get; set; }

        [Required]
        public int AuthorId { get; set; }

        [ForeignKey("AuthorId")]
        public required User Author { get; set; }

        [Required]
        public int WallOwnerId { get; set; }

        [ForeignKey("WallOwnerId")]
        public required User WallOwner { get; set; }

        public bool IsDeleted { get; set; }

        public required ICollection<Like> Likes { get; set; }
        public required ICollection<Comment> Comments { get; set; }
    }
} 