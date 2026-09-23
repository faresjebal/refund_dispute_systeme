using Internship.Domain.Entities;
using System.ComponentModel.DataAnnotations;

namespace Internship.Application.DTOs.Dispute
{
    public class ProcessDisputeDto
    {
        [Required]
        public string DisputeId { get; set; } = string.Empty;

        [Required]
        public DisputeStatus Status { get; set; }

        [StringLength(2000)]
        public string? ResolutionComments { get; set; }

        public string? AssignedToUserId { get; set; }
    }
}