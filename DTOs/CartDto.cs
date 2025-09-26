namespace ECommerce.DTOs
{
    public class CartItemDto
    {
        public int Id { get; set; }
        public string ProductName { get; set; }
        public string ProductImage { get; set; }
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public decimal Total => UnitPrice * Quantity;
    }
/*
    public class CartSummaryDto
    {
        public decimal Subtotal { get; set; }
        public decimal Shipping { get; set; } 
        public string PromoCode { get; set; }
        public decimal PromoDiscount { get; set; } 
        public decimal Total => Subtotal + Shipping - PromoDiscount;
    }
*/
    public class CartFullDto
    {
        public List<CartItemDto> Items { get; set; }
        //public CartSummaryDto Summary { get; set; }
        public decimal Subtotal { get; set; }
    }


}
