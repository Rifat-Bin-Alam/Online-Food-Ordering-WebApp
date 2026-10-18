namespace OnlineFoodOrdering.Models
{
    public class CartItem
    {
        public int FoodItemId { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Quantity { get; set; } = 1;
        public string ImagePath { get; set; } = string.Empty;
    }
}
