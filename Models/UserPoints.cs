namespace ECommerce.Models
{
    public class UserPoints
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public int TotalPoints { get; set; }
        public int AvailablePoints { get; set; }
        public DateTime LastUpdated { get; set; }

        public virtual AppUser User { get; set; }
        public virtual ICollection<PointTransaction> PointTransactions { get; set; }
    }
}
