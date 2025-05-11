using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models
{
    [Table("friends")]
    public class Friend
    {
        public Friend()
        {
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
            Status = FriendStatus.Pending;
        }

        [Key]
        public int Id { get; set; }
        
        [Required]
        public int UserId { get; set; }
        
        [Required]
        public int FriendId { get; set; }
        
        [Required]
        public FriendStatus Status { get; set; }
        
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        [ForeignKey("UserId")]
        public required User User { get; set; }
        
        [ForeignKey("FriendId")]
        public required User FriendUser { get; set; }
    }

    public enum FriendStatus
    {
        Pending = 0,
        Accepted = 1,
        Declined = 2,
        Blocked = 3
    }
} 