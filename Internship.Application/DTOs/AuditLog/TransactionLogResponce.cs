
using Internship.Domain.Entities;

namespace Internship.Application.DTOs;
public class TransactionLogResponse
{
    public int Id { get; set; }
    public string TransactionId { get; set; } = null!;
    public string PreviousStatus { get; set; } = null!;
    public string NewStatus { get; set; } = null!;
    public string? Notes { get; set; }
    public DateTime ChangedAt { get; set; }
    public string? ChangedByUserId { get; set; }
    public string ChangedBy { get; set; } = null!;
}