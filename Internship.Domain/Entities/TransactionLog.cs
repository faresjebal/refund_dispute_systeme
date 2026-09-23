// Domain/Entities/TransactionLog.cs
using System.ComponentModel.DataAnnotations;

namespace Internship.Domain.Entities
{
    public class TransactionLog
    {
        [Key]
        public int Id { get; set; }
        [Required]
        [MaxLength(50)]
         public string TransactionId { get; set; }

        [Required]
        public TransactionStatus PreviousStatus { get; set; }

        [Required]
        public TransactionStatus NewStatus { get; set; }

        [MaxLength(1000)]
        public string? Notes { get; set; }

        public string? ChangedByUserId { get; set; }

        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual Transaction Transaction { get; set; } = null!;
        public virtual ApplicationUser? ChangedByUser { get; set; }
    }
}