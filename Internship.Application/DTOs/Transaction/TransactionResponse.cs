// DTOs/Transaction/TransactionResponse.cs
using Internship.Domain.Entities;

namespace Internship.Application.DTOs.Transaction
{
    public class TransactionResponse
    {
        public string TransactionId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = string.Empty;
        public TransactionType Type { get; set; }
        public TransactionStatus Status { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public string Description { get; set; } = string.Empty;
        public string MerchantReference { get; set; } = string.Empty;
        public string? PaymentProviderTransactionId { get; set; }
        public string? PaymentProviderName { get; set; }
        public string? MaskedCardNumber { get; set; }
        public string? CardHolderName { get; set; }
        public string? CardType { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ProcessedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string? ErrorMessage { get; set; }
        public string? ErrorCode { get; set; }
        public bool IsRefundable { get; set; }
        public bool IsDisputable { get; set; }
    }
}