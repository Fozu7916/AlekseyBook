using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;

namespace backend.Models
{
    public class Comment
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public string Content { get; set; }
        
        [Required]
        public int AuthorId { get; set; }
        
        [Required]
        public int WallPostId { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public int? ParentId { get; set; }
        
        [ForeignKey("ParentId")]
        public Comment Parent { get; set; }
        
        public ICollection<Comment> Replies { get; set; }

        [ForeignKey("AuthorId")]
        public User Author { get; set; }
        
        [ForeignKey("WallPostId")]
        public WallPost WallPost { get; set; }
    }
} 