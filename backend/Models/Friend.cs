using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models
{
    public class Friend
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public int UserId { get; set; }
        
        [Required]
        public int FriendId { get; set; }
        
        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "pending"; // pending, accepted, blocked
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("UserId")]
        public User User { get; set; }
        
        [ForeignKey("FriendId")]
        public User FriendUser { get; set; }
    }
} 