using Internship.Domain.Entities;

namespace Internship.Application.DTOs.Dispute
{
    public class DisputeResponse
    {
        public string DisputeId { get; set; } = string.Empty;
        public string TransactionId { get; set; } = string.Empty;
        public string TransactionReference { get; set; } = string.Empty;
        public DisputeType Type { get; set; }
        public string Description { get; set; } = string.Empty;
        public string? AttachmentPath { get; set; }
        public DisputeStatus Status { get; set; }
        public string? ResolutionComments { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? AssignedAt { get; set; }
        public DateTime? ResolvedAt { get; set; }
        public string UserFullName { get; set; } = string.Empty;
        public string? AssignedToUserName { get; set; }
        public string UserId { get; set; } = string.Empty;
    }
}
