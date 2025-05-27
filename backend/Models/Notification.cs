using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models
{
    public enum NotificationType
    {
        Message = 0,
        Friend = 1,
        System = 2,
        Like = 3
    }

    [Table("notifications")]
    public class Notification
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public NotificationType Type { get; set; }

        [Required]
        public required string Title { get; set; }

        [Required]
        public required string Text { get; set; }

        public string? Link { get; set; }

        public bool IsRead { get; set; }

        public DateTime CreatedAt { get; set; }

        [ForeignKey("UserId")]
        public required User User { get; set; }
    }
} 