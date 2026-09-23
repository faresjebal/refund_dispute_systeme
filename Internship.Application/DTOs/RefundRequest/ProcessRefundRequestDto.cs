using Internship.Domain.Entities;
using System.ComponentModel.DataAnnotations;

namespace Internship.Application.DTOs.RefundRequest
{
    public class ProcessRefundRequestDto
    {
        [Required]
        public string RefundId { get; set; } = string.Empty;

        [Required]
        public RefundStatus Status { get; set; }

        [Range(0.01, double.MaxValue, ErrorMessage = "Approved amount must be greater than 0")]
        public decimal? ApprovedAmount { get; set; }

        [StringLength(1000)]
        public string? AdminNotes { get; set; }
    }
}