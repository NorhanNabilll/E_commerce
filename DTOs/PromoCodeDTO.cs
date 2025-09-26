using System;
using System.ComponentModel.DataAnnotations;
using ECommerce.Models;

namespace ECommerce.DTOs
{

    public class PromoCodeDto
    {
        [Required]
        [MaxLength(50)]
        public string Code { get; set; }

        [Required]
        [Range(0, 100, ErrorMessage = "Discount must be between 0 and 100")]
        public decimal Discount { get; set; }

        [Required]
        public DateTime ExpirationDate { get; set; }
        
    }
    public class ValidatePromoRequest
    {
        [Required]
        public string PromoCode { get; set; } = string.Empty;
    }

    public class PromoValidationResult
    {
        public bool IsValid { get; set; }
        public decimal DiscountAmount { get; set; }
    }
}
