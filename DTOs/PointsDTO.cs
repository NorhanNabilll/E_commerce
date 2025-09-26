using System.ComponentModel.DataAnnotations;

namespace ECommerce.DTOs
{
    // DTOs for Points Service
    public class PointsSummaryDto
    {
        public int TotalPoints { get; set; }
        public int AvailablePoints { get; set; }
        public int PointsEarnedThisMonth { get; set; }
        public int PointsUsedThisMonth { get; set; }
        public DateTime LastUpdated { get; set; }
        public decimal PointValue { get; set; }

    }

    public class PointTransactionDto
    {
        public int Id { get; set; }
        public int Points { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int? OrderId { get; set; }
        public DateTime CreatedDate { get; set; }
        public string FormattedDate => CreatedDate.ToString("MMM dd, yyyy");
        public string PointsDisplay => Points > 0 ? $"+{Points}" : Points.ToString();
        public string TypeClass => Type.ToLower() switch
        {
            "earned" => "text-success",
            "used" => "text-primary",
            "expired" => "text-danger",
            _ => "text-secondary"
        };
    }

    public class AddPointsRequest
    {
        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Points must be greater than 0")]
        public int Points { get; set; }

        [Required]
        [StringLength(500, ErrorMessage = "Description is too long")]
        public string Description { get; set; } = string.Empty;

        public int? OrderId { get; set; }
    }

    public class UsePointsRequest
    {
        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Points must be greater than 0")]
        public int Points { get; set; }

        public int? OrderId { get; set; }
    }
}
