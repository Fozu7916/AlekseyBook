using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models
{
    public class Like
    {
        public Like()
        {
            CreatedAt = DateTime.UtcNow;
        }

        [Key]
        public int Id { get; set; }
        
        [Required]
        public int UserId { get; set; }
        
        [Required]
        public int WallPostId { get; set; }
        
        public DateTime CreatedAt { get; set; }

        [ForeignKey("UserId")]
        public required User User { get; set; }
        
        [ForeignKey("WallPostId")]
        public required WallPost WallPost { get; set; }
    }
} 