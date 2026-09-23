// DTOs/RefundRequest/CreateRefundRequestDto.cs
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace Internship.Application.DTOs.RefundRequest
{
    public class CreateRefundRequestDto
    {
        [Required]
        public string TransactionId { get; set; } = string.Empty;

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Requested amount must be greater than 0")]
        public decimal RequestedAmount { get; set; }

        [Required]
        [StringLength(1000, MinimumLength = 10)]
        public string Reason { get; set; } = string.Empty;

        public IFormFile? Attachment { get; set; }
    }
}