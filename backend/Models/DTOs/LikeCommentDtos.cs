using System;

namespace backend.Models.DTOs
{
    public class LikeDto
    {
        public int Id { get; set; }
        public required UserResponseDto User { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CommentDto
    {
        public int Id { get; set; }
        public required string Content { get; set; }
        public required UserResponseDto Author { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public int? ParentId { get; set; }
        public int Likes { get; set; }
        public bool IsLiked { get; set; }
    }

    public class CreateCommentDto
    {
        public required string Content { get; set; }
        public int WallPostId { get; set; }
        public int? ParentId { get; set; }
    }

    public class UpdateCommentDto
    {
        public required string Content { get; set; }
    }
} 