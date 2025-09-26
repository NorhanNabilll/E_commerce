namespace ECommerce.Models
{
    public class CartItem
    {
        public int Id { get; set; }

        // Foreign Keys
        public int CartId { get; set; }
        public int ProductId { get; set; }

        // Properties
        public int Quantity { get; set; } = 1;

        // Navigation
        public Cart Cart { get; set; }
        public Product Product { get; set; }
    }
}
