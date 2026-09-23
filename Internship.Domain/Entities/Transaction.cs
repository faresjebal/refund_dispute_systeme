using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Internship.Domain.Entities
{
    public class Transaction
    {
        [Key]
        [Required]
        [MaxLength(50)]
        public string TransactionId { get; set; } = Guid.NewGuid().ToString();

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Required]
        [MaxLength(3)]
        public string Currency { get; set; } = "DT";

        [Required]
        public TransactionType Type { get; set; }

        [Required]
        public TransactionStatus Status { get; set; }

        [Required]
        public PaymentMethod PaymentMethod { get; set; }

        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        [MaxLength(100)]
        public string MerchantReference { get; set; } = string.Empty;

        // Payment provider simulation fields
        [MaxLength(50)]
        public string? PaymentProviderTransactionId { get; set; }

        [MaxLength(100)]
        public string? PaymentProviderName { get; set; } = "SimulatedPaymentProvider";

        // Card details (masked for security)
        [MaxLength(20)]
        public string? MaskedCardNumber { get; set; }

        [MaxLength(50)]
        public string? CardHolderName { get; set; }

        [MaxLength(20)]
        public string? CardType { get; set; }

        // Timestamps
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ProcessedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime? FailedAt { get; set; }

        // Error handling
        [MaxLength(500)]
        public string? ErrorMessage { get; set; }

        [MaxLength(50)]
        public string? ErrorCode { get; set; }

        // Navigation properties
        public virtual ApplicationUser User { get; set; } = null!;
        public virtual ICollection<RefundRequest> RefundRequests { get; set; } = new List<RefundRequest>();
        public virtual ICollection<Dispute> Disputes { get; set; } = new List<Dispute>();
        public virtual ICollection<TransactionLog> TransactionLogs { get; set; } = new List<TransactionLog>();

        // Computed properties
        [NotMapped]
        public bool IsRefundable => Status == TransactionStatus.Completed &&
                                   Type == TransactionType.Payment &&
                                   Amount > 0;

        [NotMapped]
        public bool IsDisputable => Status == TransactionStatus.Completed &&
                                   CreatedAt >= DateTime.UtcNow.AddDays(-90); // 90 days dispute window
    }

    public enum TransactionType
    {
        Payment = 1,
        Refund = 2,
        Chargeback = 3,
        Fee = 4,
        Adjustment = 5
    }

    public enum TransactionStatus
    {
        Pending = 1,
        Processing = 2,
        Completed = 3,
        Failed = 4,
        Cancelled = 5,
        Expired = 6,
        Refunded = 7,
        PartiallyRefunded = 8,
        Disputed = 9
    }

    public enum PaymentMethod
    {
        CreditCard = 1,
        DebitCard = 2,
        BankTransfer = 3,
        PayPal = 4,
        Stripe = 5,
        ApplePay = 6,
        GooglePay = 7,
        Cryptocurrency = 8
    }
}