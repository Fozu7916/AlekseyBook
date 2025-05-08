using System;

namespace backend.Models
{
    public class CommentLike
    {
        public int Id { get; set; }
        public int CommentId { get; set; }
        public int UserId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Comment Comment { get; set; }
        public User User { get; set; }
    }
} 