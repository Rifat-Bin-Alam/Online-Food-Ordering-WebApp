using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using OnlineFoodOrdering.Data;
using OnlineFoodOrdering.Models;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using Stripe;
using Stripe.Checkout;


namespace OnlineFoodOrdering.Controllers
{
    public class MainController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration; // <-- add this

        // Constructor
        public MainController(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // ============================
        // LOAD USER NOTIFICATIONS
        // ============================
        private void LoadUserNotifications()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return;

            var notifications = _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .OrderByDescending(n => n.CreatedAt)
                .ToList();

            ViewBag.Notifications = notifications;
            ViewBag.NotificationCount = notifications.Count;
        }

        // ============================
        // HOME / FOOD LIST
        // ============================
        public IActionResult Index(string search)
        {
            LoadUserNotifications();

            var items = _context.FoodItems.AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                items = items.Where(f => f.Name.ToLower().Contains(search.ToLower()));
            }

            return View(items.ToList());
        }

        // ============================
        // ADD TO CART
        // ============================
        public IActionResult AddToCart(int id)
        {
            var item = _context.FoodItems.FirstOrDefault(f => f.FoodItemId == id);
            if (item == null) return NotFound();

            var cart = new List<CartItem>();
            var sessionCart = HttpContext.Session.GetString("Cart");

            if (!string.IsNullOrEmpty(sessionCart))
                cart = JsonConvert.DeserializeObject<List<CartItem>>(sessionCart);

            var existingItem = cart.FirstOrDefault(c => c.FoodItemId == id);
            if (existingItem != null)
                existingItem.Quantity++;
            else
                cart.Add(new CartItem
                {
                    FoodItemId = item.FoodItemId,
                    Name = item.Name,
                    Price = item.Price,
                    Quantity = 1
                });

            HttpContext.Session.SetString("Cart", JsonConvert.SerializeObject(cart));

            return RedirectToAction("Cart");
        }

        // ============================
        // CART VIEW
        // ============================
        public IActionResult Cart()
        {
            var cart = new List<CartItem>();
            var sessionCart = HttpContext.Session.GetString("Cart");

            if (!string.IsNullOrEmpty(sessionCart))
                cart = JsonConvert.DeserializeObject<List<CartItem>>(sessionCart);

            return View(cart);
        }

        // ============================
        // REMOVE FROM CART
        // ============================
        public IActionResult RemoveFromCart(int id)
        {
            var sessionCart = HttpContext.Session.GetString("Cart");
            if (string.IsNullOrEmpty(sessionCart))
                return RedirectToAction("Cart");

            var cart = JsonConvert.DeserializeObject<List<CartItem>>(sessionCart);
            var item = cart.FirstOrDefault(c => c.FoodItemId == id);

            if (item != null)
                cart.Remove(item);

            HttpContext.Session.SetString("Cart", JsonConvert.SerializeObject(cart));
            return RedirectToAction("Cart");
        }

        // ============================
        // CHECKOUT (GET)
        // ============================
        public IActionResult Checkout()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToAction("Login", "Account");

            var sessionCart = HttpContext.Session.GetString("Cart");
            if (string.IsNullOrEmpty(sessionCart))
                return RedirectToAction("Index");

            var cart = JsonConvert.DeserializeObject<List<CartItem>>(sessionCart);
            if (cart.Count == 0)
                return RedirectToAction("Index");

            return View(cart);
        }

        // ============================
        // CHECKOUT (POST)
        // ============================
        [HttpPost]
        public IActionResult Checkout(string customerName, string phoneNumber, string address, string paymentMethod)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToAction("Login", "Account");

            var sessionCart = HttpContext.Session.GetString("Cart");
            if (string.IsNullOrEmpty(sessionCart))
                return RedirectToAction("Index");

            var cart = JsonConvert.DeserializeObject<List<CartItem>>(sessionCart);
            if (cart.Count == 0)
                return RedirectToAction("Index");

            // ================= CASH ON DELIVERY =================
            if (paymentMethod == "Cash On Delivery")
            {
                var order = new Order
                {
                    UserId = userId.Value,
                    CustomerName = customerName,
                    PhoneNumber = phoneNumber,
                    Address = address,
                    PaymentMethod = paymentMethod,
                    Status = "Processing",
                    OrderDate = DateTime.Now
                };

                _context.Orders.Add(order);
                _context.SaveChanges();

                foreach (var item in cart)
                {
                    _context.OrderItems.Add(new OrderItem
                    {
                        OrderId = order.OrderId,
                        FoodItemId = item.FoodItemId,
                        Quantity = item.Quantity
                    });
                }

                _context.SaveChanges();
                HttpContext.Session.Remove("Cart");

                TempData["Success"] = "Order placed successfully!";
                return RedirectToAction("MyOrders");
            }

            // ================= STRIPE ONLINE PAYMENT =================
            // Set Stripe secret key
            StripeConfiguration.ApiKey = _configuration["Stripe:SecretKey"];

            // Save customer info in session to use after payment
            var customerInfo = new
            {
                Name = customerName,
                Phone = phoneNumber,
                Address = address
            };
            HttpContext.Session.SetString("StripeCustomerInfo", JsonConvert.SerializeObject(customerInfo));

            // Prepare line items for Stripe
            var lineItems = cart.Select(item => new SessionLineItemOptions
            {
                PriceData = new SessionLineItemPriceDataOptions
                {
                    Currency = "usd",
                    UnitAmount = (long)(item.Price * 100), // Stripe requires amount in cents
                    ProductData = new SessionLineItemPriceDataProductDataOptions
                    {
                        Name = item.Name
                    }
                },
                Quantity = item.Quantity
            }).ToList();

            // Create Stripe session
            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                Mode = "payment",
                LineItems = lineItems,
                SuccessUrl = Url.Action("StripeSuccess", "Main", null, Request.Scheme),
                CancelUrl = Url.Action("StripeCancel", "Main", null, Request.Scheme)
            };

            var service = new SessionService();
            var session = service.Create(options);

            // Redirect user to Stripe checkout
            return Redirect(session.Url);
        }

        // ================= STRIPE SUCCESS =================
        public IActionResult StripeSuccess()
        {
            var sessionCart = HttpContext.Session.GetString("Cart");
            if (string.IsNullOrEmpty(sessionCart))
                return RedirectToAction("Index");

            var cart = JsonConvert.DeserializeObject<List<CartItem>>(sessionCart);
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToAction("Login", "Account");

            // Retrieve customer info from session
            var customerInfoJson = HttpContext.Session.GetString("StripeCustomerInfo");
            var customerInfo = !string.IsNullOrEmpty(customerInfoJson)
                ? JsonConvert.DeserializeObject<dynamic>(customerInfoJson)
                : null;

            // Create order with actual customer info
            var order = new Order
            {
                UserId = userId.Value,
                CustomerName = customerInfo?.Name ?? "Stripe Payment",
                PhoneNumber = customerInfo?.Phone ?? "N/A",
                Address = customerInfo?.Address ?? "Paid via Stripe",
                PaymentMethod = "Online Payment",
                Status = "Confirmed",
                OrderDate = DateTime.Now
            };

            _context.Orders.Add(order);
            _context.SaveChanges();

            // Add items to order
            foreach (var item in cart)
            {
                _context.OrderItems.Add(new OrderItem
                {
                    OrderId = order.OrderId,
                    FoodItemId = item.FoodItemId,
                    Quantity = item.Quantity
                });
            }

            _context.SaveChanges();

            // Clear cart and customer info session
            HttpContext.Session.Remove("Cart");
            HttpContext.Session.Remove("StripeCustomerInfo");

            TempData["Success"] = "Payment successful! Order confirmed.";
            return RedirectToAction("MyOrders");
        }

        // ================= STRIPE CANCEL =================
        public IActionResult StripeCancel()
        {
            TempData["Error"] = "Payment was cancelled.";
            return RedirectToAction("Cart");
        }




        // ============================
        // MY ORDERS (USER)
        // ============================
        public IActionResult MyOrders()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToAction("Login", "Account");

            LoadUserNotifications();

            // Include OrderItems and then each FoodItem
            var orders = _context.Orders
                .Where(o => o.UserId == userId)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.FoodItem)
                .OrderByDescending(o => o.OrderDate)
                .ToList();

            return View(orders);
        }


        // ============================
        // MARK NOTIFICATION AS READ
        // ============================
        public IActionResult MarkNotificationRead(int id)
        {
            var notification = _context.Notifications.FirstOrDefault(n => n.Id == id);
            if (notification != null)
            {
                notification.IsRead = true;
                _context.SaveChanges();
            }

            return RedirectToAction("Index");
        }
    }
}
