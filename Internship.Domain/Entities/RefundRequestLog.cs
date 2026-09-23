using System.ComponentModel.DataAnnotations;

namespace Internship.Domain.Entities
{
    public class RefundRequestLog
    {
        [Key]
         public int Id { get; set; }
        [Required]
        [MaxLength(50)]
        public string RefundId { get; set; }

        [Required]
        public RefundStatus PreviousStatus { get; set; }

        [Required]
        public RefundStatus NewStatus { get; set; }

        [MaxLength(1000)]
        public string? Notes { get; set; }

        public string? ChangedByUserId { get; set; }

        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual RefundRequest RefundRequest { get; set; } = null!;
        public virtual ApplicationUser? ChangedByUser { get; set; }
    }
}