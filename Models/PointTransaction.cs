namespace ECommerce.Models
{
    public class PointTransaction
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public int Points { get; set; }
        public PointTransactionType Type { get; set; } // Earned, Used, Expired
        public string Description { get; set; }
        public int? OrderId { get; set; }
        public DateTime CreatedDate { get; set; }

        public virtual AppUser User { get; set; }
        public virtual Order? Order { get; set; }
        public  UserPoints UserPoints { get; set; }
    }
    public enum PointTransactionType
    {
        Earned,
        Used,
        Expired
    }
    
}
