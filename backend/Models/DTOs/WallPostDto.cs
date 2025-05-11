using System;

namespace backend.Models.DTOs
{
    public class WallPostDto
    {
        public int Id { get; set; }
        public required string Content { get; set; }
        public string? ImageUrl { get; set; }
        public DateTime CreatedAt { get; set; }
        public int AuthorId { get; set; }
        public required string AuthorName { get; set; }
        public required string AuthorAvatarUrl { get; set; }
        public int WallOwnerId { get; set; }
    }

    public class CreateWallPostDto
    {
        public required string Content { get; set; }
        public string? ImageUrl { get; set; }
        public int WallOwnerId { get; set; }
    }

    public class UpdateWallPostDto
    {
        public required string Content { get; set; }
        public string? ImageUrl { get; set; }
    }
} 