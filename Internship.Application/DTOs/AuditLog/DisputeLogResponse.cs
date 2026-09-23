using Internship.Domain.Entities;

namespace Internship.Application.DTOs;

public class DisputeLogResponse


{
    public int Id { get; set; }
    public string DisputeId { get; set; } = null!;
    public string PreviousStatus { get; set; } = null!;
    public string NewStatus { get; set; } = null!;
    public string? Notes { get; set; }
    public DateTime ChangedAt { get; set; }
    public string? ChangedByUserId { get; set; }
    public string ChangedBy { get; set; } = null!;
}