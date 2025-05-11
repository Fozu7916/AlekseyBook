using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models
{
    public class CommentLike
    {
        public CommentLike()
        {
            CreatedAt = DateTime.UtcNow;
        }

        [Key]
        public int Id { get; set; }

        [Required]
        public int CommentId { get; set; }

        [Required]
        public int UserId { get; set; }

        public DateTime CreatedAt { get; set; }

        [ForeignKey("CommentId")]
        public required Comment Comment { get; set; }

        [ForeignKey("UserId")]
        public required User User { get; set; }
    }
} 