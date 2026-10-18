using System.Linq;
using OnlineFoodOrdering.Models;

namespace OnlineFoodOrdering.Data
{
    public static class DbInitializer
    {
        public static void Initialize(ApplicationDbContext context)
        {
            // Ensure database is created and schema exists
            context.Database.EnsureCreated();

            bool hasChanges = false;

            // 1. Ensure Customer Account
            if (!context.Users.Any(u => u.Email == "test@gmail.com"))
            {
                context.Users.Add(new User
                {
                    FullName = "Test Customer",
                    Email = "test@gmail.com",
                    Password = "1234",
                    PhoneNumber = "+1 555-0199",
                    IsAdmin = false
                });
                hasChanges = true;
            }

            // 2. Ensure Administrator Account
            if (!context.Users.Any(u => u.Email == "rifat@gmail.com"))
            {
                context.Users.Add(new User
                {
                    FullName = "Rifat (Admin)",
                    Email = "rifat@gmail.com",
                    Password = "11223344",
                    PhoneNumber = "+1 555-0100",
                    IsAdmin = true
                });
                hasChanges = true;
            }

            // 3. Ensure Sample Menu Items if table is empty
            if (!context.FoodItems.Any())
            {
                var sampleFoodItems = new[]
                {
                    new FoodItem
                    {
                        Name = "Gourmet Artisan Burger",
                        Price = 12.99m,
                        ImagePath = "/images/burger.jpg"
                    },
                    new FoodItem
                    {
                        Name = "Classic Italian Margherita Pizza",
                        Price = 15.49m,
                        ImagePath = "/images/pizza.jpg"
                    },
                    new FoodItem
                    {
                        Name = "Creamy Truffle Alfredo Pasta",
                        Price = 14.29m,
                        ImagePath = "/images/pasta.jpg"
                    },
                    new FoodItem
                    {
                        Name = "Toasted Deli Club Sandwich",
                        Price = 9.99m,
                        ImagePath = "/images/sandwich.jpg"
                    },
                    new FoodItem
                    {
                        Name = "Belgian Chocolate Decadence Cake",
                        Price = 7.99m,
                        ImagePath = "/images/cake.jpg"
                    },
                    new FoodItem
                    {
                        Name = "Handcrafted Gelato Ice Cream",
                        Price = 5.99m,
                        ImagePath = "/images/icecream.jpg"
                    },
                    new FoodItem
                    {
                        Name = "Crispy Golden Mozzarella Bites",
                        Price = 8.49m,
                        ImagePath = "/images/images.jpeg"
                    },
                    new FoodItem
                    {
                        Name = "Glazed Honey Garlic Chicken",
                        Price = 13.99m,
                        ImagePath = "/images/product1.jpg"
                    }
                };

                context.FoodItems.AddRange(sampleFoodItems);
                hasChanges = true;
            }

            if (hasChanges)
            {
                context.SaveChanges();
            }
        }
    }
}
