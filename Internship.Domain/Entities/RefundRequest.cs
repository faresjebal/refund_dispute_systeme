using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Internship.Domain.Entities
{
    public class RefundRequest
    {
        [Key]
        [Required]
        [MaxLength(50)]
        public string RefundId { get; set; } = Guid.NewGuid().ToString();

        [Required]
        [MaxLength(50)]
        public string TransactionId { get; set; } = string.Empty;

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal RequestedAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? ApprovedAmount { get; set; }

        [Required]
        [MaxLength(1000)]
        public string Reason { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? AttachmentPath { get; set; }

        [Required]
        public RefundStatus Status { get; set; } = RefundStatus.Pending;

        [MaxLength(1000)]
        public string? AdminNotes { get; set; }

        public string? ProcessedByUserId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ProcessedAt { get; set; }

        // Navigation properties
        public virtual Transaction Transaction { get; set; } = null!;

        public virtual ApplicationUser User { get; set; } = null!;
        public virtual ApplicationUser? ProcessedByUser { get; set; }
        public virtual ICollection<RefundRequestLog> RefundLogs { get; set; } = new List<RefundRequestLog>();
    }

    public enum RefundStatus
    {
        Pending = 1,
        UnderReview = 2,
        Approved = 3,
        Rejected = 4,
        Completed = 5,
        Failed = 6
    }
}