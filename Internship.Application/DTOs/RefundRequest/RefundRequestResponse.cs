using Internship.Domain.Entities;

namespace Internship.Application.DTOs.RefundRequest
{
    public class RefundRequestResponse
    {
        public string RefundId { get; set; } = string.Empty;
        public string TransactionId { get; set; } = string.Empty;
        public string TransactionReference { get; set; } = string.Empty;
        public decimal RequestedAmount { get; set; }
        public decimal? ApprovedAmount { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string? AttachmentPath { get; set; }
        public RefundStatus Status { get; set; }
        public string? AdminNotes { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ProcessedAt { get; set; }
        public string UserFullName { get; set; } = string.Empty;
        public string? ProcessedByUserName { get; set; }
    }
}