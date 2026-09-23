using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Internship.Domain.Entities
{
    public class Dispute
    {
        [Key]
        [Required]
        [MaxLength(50)]
        public string DisputeId { get; set; } = Guid.NewGuid().ToString();

        [Required]
        [MaxLength(50)]
        public string TransactionId { get; set; } = string.Empty;

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        public DisputeType Type { get; set; }

        [Required]
        [MaxLength(2000)]
        public string Description { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? AttachmentPath { get; set; }

        [Required]
        public DisputeStatus Status { get; set; } = DisputeStatus.Open;

        [MaxLength(2000)]
        public string? ResolutionComments { get; set; }

        public string? AssignedToUserId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? AssignedAt { get; set; }
        public DateTime? ResolvedAt { get; set; }

        // Navigation properties
        public virtual Transaction Transaction { get; set; } = null!;

        public virtual ApplicationUser User { get; set; } = null!;
        public virtual ApplicationUser? AssignedToUser { get; set; }
        public virtual ICollection<DisputeLog> DisputeLogs { get; set; } = new List<DisputeLog>();
    }

    public enum DisputeType
    {
        UnauthorizedTransaction = 1,
        ServiceNotReceived = 2,
        ServiceNotAsDescribed = 3,
        DuplicateCharge = 4,
        TechnicalIssue = 5,
        BillingError = 6,   
        Other = 7
    }

    public enum DisputeStatus
    {
        Open = 1,
        UnderReview = 2,
        AwaitingCustomerResponse = 3,
        AwaitingMerchantResponse = 4,
        Resolved = 5,
        Rejected = 6,
        Escalated = 7
    }
}