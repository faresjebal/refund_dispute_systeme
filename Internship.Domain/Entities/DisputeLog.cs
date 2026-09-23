using System.ComponentModel.DataAnnotations;

namespace Internship.Domain.Entities
{
    public class DisputeLog
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string DisputeId { get; set; }

        [Required]
        public DisputeStatus PreviousStatus { get; set; }

        [Required]
        public DisputeStatus NewStatus { get; set; }

        [MaxLength(1000)]
        public string? Notes { get; set; }

        public string? ChangedByUserId { get; set; }

        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual Dispute Dispute { get; set; } = null!;
        public virtual ApplicationUser? ChangedByUser { get; set; }
    }
}