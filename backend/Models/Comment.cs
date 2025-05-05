using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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

        [ForeignKey("AuthorId")]
        public User Author { get; set; }
        
        [ForeignKey("WallPostId")]
        public WallPost WallPost { get; set; }
    }
} 