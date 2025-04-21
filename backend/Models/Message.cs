using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models
{
    [Table("messages")]
    public class Message
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public int SenderId { get; set; }
        
        [Required]
        public int ReceiverId { get; set; }
        
        [Required]
        public string Content { get; set; }
        
        public bool IsRead { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        [ForeignKey("SenderId")]
        public User Sender { get; set; }
        
        [ForeignKey("ReceiverId")]
        public User Receiver { get; set; }
    }
} 