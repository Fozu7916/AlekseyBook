using System;

namespace backend.Models.DTOs
{
    public class WallPostDto
    {
        public int Id { get; set; }
        public string Content { get; set; }
        public string? ImageUrl { get; set; }
        public DateTime CreatedAt { get; set; }
        public int AuthorId { get; set; }
        public string AuthorName { get; set; }
        public string AuthorAvatarUrl { get; set; }
        public int WallOwnerId { get; set; }
    }

    public class CreateWallPostDto
    {
        public string Content { get; set; }
        public string? ImageUrl { get; set; }
        public int WallOwnerId { get; set; }
    }

    public class UpdateWallPostDto
    {
        public string Content { get; set; }
        public string? ImageUrl { get; set; }
    }
} 