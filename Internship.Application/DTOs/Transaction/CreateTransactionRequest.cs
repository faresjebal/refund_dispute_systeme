using System.ComponentModel.DataAnnotations;
using Internship.Domain.Entities;


namespace Internship.Application.DTOs.Transaction
{
    public class CreateTransactionRequest
    {
        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
        public decimal Amount { get; set; }

        [Required]
        [StringLength(3, MinimumLength = 3)]
        public string Currency { get; set; } = "DT";

        [Required]
        public PaymentMethod PaymentMethod { get; set; }

        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        [StringLength(100)]
        public string MerchantReference { get; set; } = string.Empty;

        // Card details for processing
        [Required]
        [StringLength(19, MinimumLength = 13)]
        public string CardNumber { get; set; } = string.Empty;

        [StringLength(50)]
        public string CardHolderName { get; set; } = string.Empty;

        [Required]
        [StringLength(3, MinimumLength = 3)]
        public string ExpiryMonth { get; set; } = string.Empty;

        [Required]
        [StringLength(4, MinimumLength = 2)]
        public string ExpiryYear { get; set; } = string.Empty;

        [Required]
        [StringLength(4, MinimumLength = 3)]
        public string CVV { get; set; } = string.Empty;
    }
}