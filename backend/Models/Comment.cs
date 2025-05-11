using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;

namespace backend.Models
{
    public class Comment
    {
        public Comment()
        {
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
            Replies = new List<Comment>();
        }

        [Key]
        public int Id { get; set; }
        
        [Required]
        public required string Content { get; set; }
        
        [Required]
        public int AuthorId { get; set; }
        
        [Required]
        public int WallPostId { get; set; }
        
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public int? ParentId { get; set; }
        
        [ForeignKey("ParentId")]
        public Comment? Parent { get; set; }
        
        public required ICollection<Comment> Replies { get; set; }

        [ForeignKey("AuthorId")]
        public required User Author { get; set; }
        
        [ForeignKey("WallPostId")]
        public required WallPost WallPost { get; set; }
    }
} 