using System;

namespace backend.Models.DTOs
{
    public class LikeDto
    {
        public int Id { get; set; }
        public UserResponseDto User { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CommentDto
    {
        public int Id { get; set; }
        public string Content { get; set; }
        public UserResponseDto Author { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class CreateCommentDto
    {
        public string Content { get; set; }
        public int WallPostId { get; set; }
        public int? ParentId { get; set; }
    }

    public class UpdateCommentDto
    {
        public string Content { get; set; }
    }
} 