using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models
{
    public class Track
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [StringLength(255)]
        public string Title { get; set; }
        
        [Required]
        [StringLength(255)]
        public string Artist { get; set; }
        
        [StringLength(255)]
        public string Album { get; set; }
        
        public int Duration { get; set; } // в секундах
        
        public string CoverUrl { get; set; }
        
        [Required]
        public string FileUrl { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<UserTrack> UserTracks { get; set; }
        public ICollection<PlaylistTrack> PlaylistTracks { get; set; }
    }

    public class UserTrack
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public int UserId { get; set; }
        
        [Required]
        public int TrackId { get; set; }
        
        public bool IsFavorite { get; set; }
        
        public int PlayCount { get; set; }
        
        public DateTime? LastPlayed { get; set; }

        [ForeignKey("UserId")]
        public User User { get; set; }
        
        [ForeignKey("TrackId")]
        public Track Track { get; set; }
    }

    public class Playlist
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [StringLength(255)]
        public string Name { get; set; }
        
        [Required]
        public int UserId { get; set; }
        
        public bool IsPublic { get; set; } = true;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("UserId")]
        public User User { get; set; }
        
        public ICollection<PlaylistTrack> Tracks { get; set; }
    }

    public class PlaylistTrack
    {
        [Required]
        public int PlaylistId { get; set; }
        
        [Required]
        public int TrackId { get; set; }
        
        public int Position { get; set; }
        
        public DateTime AddedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("PlaylistId")]
        public Playlist Playlist { get; set; }
        
        [ForeignKey("TrackId")]
        public Track Track { get; set; }
    }
} 