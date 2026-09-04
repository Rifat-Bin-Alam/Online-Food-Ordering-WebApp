using System.ComponentModel.DataAnnotations;

namespace OnlineFoodOrdering.Models
{
    public class FoodItem
    {
        [Key]
        public int FoodItemId { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        public decimal Price { get; set; }

        public string ImagePath { get; set; }
    }
}
