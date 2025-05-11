using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models
{
    public class Track
    {
        public Track()
        {
            CreatedAt = DateTime.UtcNow;
            UserTracks = new List<UserTrack>();
            PlaylistTracks = new List<PlaylistTrack>();
        }

        [Key]
        public int Id { get; set; }
        
        [Required]
        [StringLength(255)]
        public required string Title { get; set; }
        
        [Required]
        [StringLength(255)]
        public required string Artist { get; set; }
        
        [StringLength(255)]
        public required string Album { get; set; }
        
        public int Duration { get; set; } // в секундах
        
        public required string CoverUrl { get; set; }
        
        [Required]
        public required string FileUrl { get; set; }
        
        public DateTime CreatedAt { get; set; }

        public required ICollection<UserTrack> UserTracks { get; set; }
        public required ICollection<PlaylistTrack> PlaylistTracks { get; set; }
    }

    public class UserTrack
    {
        public UserTrack()
        {
            PlayCount = 0;
            IsFavorite = false;
        }

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
        public required User User { get; set; }
        
        [ForeignKey("TrackId")]
        public required Track Track { get; set; }
    }

    public class Playlist
    {
        public Playlist()
        {
            CreatedAt = DateTime.UtcNow;
            IsPublic = true;
            Tracks = new List<PlaylistTrack>();
        }

        [Key]
        public int Id { get; set; }
        
        [Required]
        [StringLength(255)]
        public required string Name { get; set; }
        
        [Required]
        public int UserId { get; set; }
        
        public bool IsPublic { get; set; }
        
        public DateTime CreatedAt { get; set; }

        [ForeignKey("UserId")]
        public required User User { get; set; }
        
        public required ICollection<PlaylistTrack> Tracks { get; set; }
    }

    public class PlaylistTrack
    {
        public PlaylistTrack()
        {
            AddedAt = DateTime.UtcNow;
            Position = 0;
        }

        [Required]
        public int PlaylistId { get; set; }
        
        [Required]
        public int TrackId { get; set; }
        
        public int Position { get; set; }
        
        public DateTime AddedAt { get; set; }

        [ForeignKey("PlaylistId")]
        public required Playlist Playlist { get; set; }
        
        [ForeignKey("TrackId")]
        public required Track Track { get; set; }
    }
} 