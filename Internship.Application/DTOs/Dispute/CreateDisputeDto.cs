using Internship.Domain.Entities;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace Internship.Application.DTOs.Dispute
{
    public class CreateDisputeDto
    {
        [Required]
        public string TransactionId { get; set; } = string.Empty;

        [Required]
        public DisputeType Type { get; set; }

        [Required]
        [StringLength(2000, MinimumLength = 20)]
        public string Description { get; set; } = string.Empty;

        public IFormFile? Attachment { get; set; }
    }
}