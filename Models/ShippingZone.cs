namespace ECommerce.Models
{
    public class ShippingZone
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public double CenterLatitude { get; set; }
        public double CenterLongitude { get; set; }
        public double RadiusKm { get; set; }
        public decimal ShippingCost { get; set; }
        public bool IsActive { get; set; }
    }
}
