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
        private readonly IConfiguration _configuration;

        public MainController(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // ============================
        // HELPER: LOAD USER NOTIFICATIONS
        // ============================
        private void LoadUserNotifications()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                ViewBag.Notifications = new List<Notification>();
                ViewBag.NotificationCount = 0;
                return;
            }

            var notifications = _context.Notifications
                .Where(n => n.UserId == userId.Value && !n.IsRead)
                .OrderByDescending(n => n.CreatedAt)
                .Take(10)
                .ToList();

            ViewBag.Notifications = notifications;
            ViewBag.NotificationCount = notifications.Count;
        }

        // ============================
        // HELPER: GET SESSION CART
        // ============================
        private List<CartItem> GetSessionCart()
        {
            var sessionCart = HttpContext.Session.GetString("Cart");
            if (string.IsNullOrEmpty(sessionCart))
                return new List<CartItem>();

            try
            {
                return JsonConvert.DeserializeObject<List<CartItem>>(sessionCart) ?? new List<CartItem>();
            }
            catch
            {
                return new List<CartItem>();
            }
        }

        private void SaveSessionCart(List<CartItem> cart)
        {
            HttpContext.Session.SetString("Cart", JsonConvert.SerializeObject(cart));
        }

        // ============================
        // HOME / FOOD LIST
        // ============================
        public IActionResult Index(string? search, string? category)
        {
            LoadUserNotifications();

            var items = _context.FoodItems.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                items = items.Where(f => f.Name.ToLower().Contains(term));
                ViewData["CurrentSearch"] = search;
            }

            if (!string.IsNullOrWhiteSpace(category) && category != "All")
            {
                var catTerm = category.Trim().ToLower();
                items = items.Where(f => f.Name.ToLower().Contains(catTerm));
                ViewData["CurrentCategory"] = category;
            }

            var cart = GetSessionCart();
            ViewBag.CartCount = cart.Sum(i => i.Quantity);
            ViewBag.CartTotal = cart.Sum(i => i.Price * i.Quantity);

            return View(items.ToList());
        }

        // ============================
        // ADD TO CART (Standard GET fallback)
        // ============================
        public IActionResult AddToCart(int id)
        {
            var item = _context.FoodItems.FirstOrDefault(f => f.FoodItemId == id);
            if (item == null) return NotFound();

            var cart = GetSessionCart();
            var existingItem = cart.FirstOrDefault(c => c.FoodItemId == id);

            if (existingItem != null)
            {
                existingItem.Quantity++;
            }
            else
            {
                cart.Add(new CartItem
                {
                    FoodItemId = item.FoodItemId,
                    Name = item.Name,
                    Price = item.Price,
                    Quantity = 1,
                    ImagePath = !string.IsNullOrEmpty(item.ImagePath) ? item.ImagePath : "/images/burger.jpg"
                });
            }

            SaveSessionCart(cart);
            TempData["Success"] = $"Added \"{item.Name}\" to your cart!";

            return RedirectToAction("Index");
        }

        // ============================
        // ADD TO CART (AJAX POST)
        // ============================
        [HttpPost]
        public IActionResult AddToCartAjax(int id)
        {
            var item = _context.FoodItems.FirstOrDefault(f => f.FoodItemId == id);
            if (item == null)
            {
                return Json(new { success = false, message = "Food item not found." });
            }

            var cart = GetSessionCart();
            var existingItem = cart.FirstOrDefault(c => c.FoodItemId == id);

            if (existingItem != null)
            {
                existingItem.Quantity++;
                // Sync latest price and image
                existingItem.Price = item.Price;
                existingItem.ImagePath = !string.IsNullOrEmpty(item.ImagePath) ? item.ImagePath : existingItem.ImagePath;
            }
            else
            {
                cart.Add(new CartItem
                {
                    FoodItemId = item.FoodItemId,
                    Name = item.Name,
                    Price = item.Price,
                    Quantity = 1,
                    ImagePath = !string.IsNullOrEmpty(item.ImagePath) ? item.ImagePath : "/images/burger.jpg"
                });
            }

            SaveSessionCart(cart);

            var totalCount = cart.Sum(c => c.Quantity);
            var grandTotal = cart.Sum(c => c.Price * c.Quantity);

            return Json(new
            {
                success = true,
                message = $"\"{item.Name}\" added to cart!",
                cartCount = totalCount,
                cartTotal = grandTotal,
                items = cart
            });
        }

        // ============================
        // UPDATE QUANTITY (AJAX POST)
        // ============================
        [HttpPost]
        public IActionResult UpdateQuantityAjax(int id, int delta)
        {
            var cart = GetSessionCart();
            var item = cart.FirstOrDefault(c => c.FoodItemId == id);

            if (item == null)
            {
                return Json(new { success = false, message = "Item not in cart." });
            }

            bool itemRemoved = false;
            item.Quantity += delta;

            if (item.Quantity <= 0)
            {
                cart.Remove(item);
                itemRemoved = true;
            }

            SaveSessionCart(cart);

            var totalCount = cart.Sum(c => c.Quantity);
            var grandTotal = cart.Sum(c => c.Price * c.Quantity);

            return Json(new
            {
                success = true,
                itemRemoved = itemRemoved,
                foodItemId = id,
                newQuantity = itemRemoved ? 0 : item.Quantity,
                itemTotal = itemRemoved ? 0 : (item.Price * item.Quantity),
                cartCount = totalCount,
                cartTotal = grandTotal,
                items = cart
            });
        }

        // ============================
        // REMOVE FROM CART (Standard GET)
        // ============================
        public IActionResult RemoveFromCart(int id)
        {
            var cart = GetSessionCart();
            var item = cart.FirstOrDefault(c => c.FoodItemId == id);

            if (item != null)
            {
                cart.Remove(item);
                SaveSessionCart(cart);
                TempData["Success"] = $"Removed \"{item.Name}\" from cart.";
            }

            return RedirectToAction("Cart");
        }

        // ============================
        // REMOVE FROM CART (AJAX POST)
        // ============================
        [HttpPost]
        public IActionResult RemoveFromCartAjax(int id)
        {
            var cart = GetSessionCart();
            var item = cart.FirstOrDefault(c => c.FoodItemId == id);

            if (item != null)
            {
                cart.Remove(item);
                SaveSessionCart(cart);
            }

            var totalCount = cart.Sum(c => c.Quantity);
            var grandTotal = cart.Sum(c => c.Price * c.Quantity);

            return Json(new
            {
                success = true,
                foodItemId = id,
                cartCount = totalCount,
                cartTotal = grandTotal,
                items = cart
            });
        }

        // ============================
        // GET CART DRAWER DATA (JSON)
        // ============================
        [HttpGet]
        public IActionResult GetCartDrawerJson()
        {
            var cart = GetSessionCart();
            return Json(new
            {
                cartCount = cart.Sum(c => c.Quantity),
                cartTotal = cart.Sum(c => c.Price * c.Quantity),
                items = cart
            });
        }

        // ============================
        // CART VIEW
        // ============================
        public IActionResult Cart()
        {
            LoadUserNotifications();
            var cart = GetSessionCart();
            return View(cart);
        }

        // ============================
        // CHECKOUT (GET)
        // ============================
        public IActionResult Checkout()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                TempData["Error"] = "Please log in to complete your checkout.";
                return RedirectToAction("Login", "Account");
            }

            LoadUserNotifications();

            var cart = GetSessionCart();
            if (cart.Count == 0)
            {
                TempData["Error"] = "Your shopping cart is empty.";
                return RedirectToAction("Index");
            }

            // Pre-populate with current user details if available
            var user = _context.Users.FirstOrDefault(u => u.UserId == userId.Value);
            ViewBag.CustomerName = user?.FullName ?? "";
            ViewBag.PhoneNumber = user?.PhoneNumber ?? "";

            return View(cart);
        }

        // ============================
        // CHECKOUT (POST)
        // ============================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Checkout(string customerName, string phoneNumber, string address, string paymentMethod)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var cart = GetSessionCart();
            if (cart.Count == 0)
            {
                TempData["Error"] = "Your cart is empty.";
                return RedirectToAction("Index");
            }

            // ================= CASH ON DELIVERY =================
            if (paymentMethod == "Cash On Delivery" || paymentMethod == "COD")
            {
                var order = new Order
                {
                    UserId = userId.Value,
                    CustomerName = customerName?.Trim() ?? "Valued Customer",
                    PhoneNumber = phoneNumber?.Trim() ?? "N/A",
                    Address = address?.Trim() ?? "Delivery Address",
                    PaymentMethod = "Cash On Delivery",
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

                // Add real notification for customer
                _context.Notifications.Add(new Notification
                {
                    UserId = userId.Value,
                    Message = $"Order #{order.OrderId} placed successfully! Status: Processing.",
                    IsRead = false,
                    CreatedAt = DateTime.Now
                });

                _context.SaveChanges();

                // Clear session cart
                HttpContext.Session.Remove("Cart");

                TempData["Success"] = $"Order #{order.OrderId} placed successfully! We're preparing your food.";
                return RedirectToAction("MyOrders");
            }

            // ================= STRIPE ONLINE PAYMENT =================
            var secretKey = _configuration["Stripe:SecretKey"];

            // Check if Stripe key is configured
            if (string.IsNullOrWhiteSpace(secretKey) || secretKey.Contains("your_stripe_secret_key"))
            {
                TempData["Error"] = "Stripe credentials are not configured on this server. Please choose 'Cash On Delivery' to place your order.";
                return RedirectToAction("Checkout");
            }

            try
            {
                StripeConfiguration.ApiKey = secretKey;

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

                return Redirect(session.Url);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Payment initiation failed: {ex.Message}. You can use Cash On Delivery.";
                return RedirectToAction("Checkout");
            }
        }

        // ================= STRIPE SUCCESS =================
        public IActionResult StripeSuccess()
        {
            var cart = GetSessionCart();
            if (cart.Count == 0)
                return RedirectToAction("Index");

            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToAction("Login", "Account");

            var customerInfoJson = HttpContext.Session.GetString("StripeCustomerInfo");
            dynamic? customerInfo = !string.IsNullOrEmpty(customerInfoJson)
                ? JsonConvert.DeserializeObject<dynamic>(customerInfoJson)
                : null;

            var order = new Order
            {
                UserId = userId.Value,
                CustomerName = customerInfo?.Name ?? "Stripe Customer",
                PhoneNumber = customerInfo?.Phone ?? "N/A",
                Address = customerInfo?.Address ?? "Paid via Stripe",
                PaymentMethod = "Online Payment (Stripe)",
                Status = "Confirmed",
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

            // Add real confirmed notification
            _context.Notifications.Add(new Notification
            {
                UserId = userId.Value,
                Message = $"Payment confirmed for Order #{order.OrderId}! Your food is being prepared.",
                IsRead = false,
                CreatedAt = DateTime.Now
            });

            _context.SaveChanges();

            // Clear session cart and customer info
            HttpContext.Session.Remove("Cart");
            HttpContext.Session.Remove("StripeCustomerInfo");

            TempData["Success"] = $"Payment successful! Your order #{order.OrderId} is confirmed.";
            return RedirectToAction("MyOrders");
        }

        // ================= STRIPE CANCEL =================
        public IActionResult StripeCancel()
        {
            TempData["Error"] = "Payment was cancelled. Your cart has been saved so you can try again or select Cash on Delivery.";
            return RedirectToAction("Cart");
        }

        // ============================
        // MY ORDERS (USER)
        // ============================
        public IActionResult MyOrders()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                TempData["Error"] = "Please log in to view your orders.";
                return RedirectToAction("Login", "Account");
            }

            LoadUserNotifications();

            var orders = _context.Orders
                .Where(o => o.UserId == userId.Value)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.FoodItem)
                .OrderByDescending(o => o.OrderDate)
                .ToList();

            return View(orders);
        }

        // ============================
        // NOTIFICATIONS: MARK ONE READ (GET fallback)
        // ============================
        public IActionResult MarkNotificationRead(int id)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId != null)
            {
                var notification = _context.Notifications.FirstOrDefault(n => n.Id == id && n.UserId == userId.Value);
                if (notification != null)
                {
                    notification.IsRead = true;
                    _context.SaveChanges();
                }
            }

            return RedirectToAction("MyOrders");
        }

        // ============================
        // NOTIFICATIONS: MARK ONE READ (AJAX POST)
        // ============================
        [HttpPost]
        public IActionResult MarkNotificationReadAjax(int id)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return Json(new { success = false, message = "Not authenticated." });
            }

            var notification = _context.Notifications.FirstOrDefault(n => n.Id == id && n.UserId == userId.Value);
            if (notification != null)
            {
                notification.IsRead = true;
                _context.SaveChanges();
            }

            var unreadCount = _context.Notifications.Count(n => n.UserId == userId.Value && !n.IsRead);

            return Json(new
            {
                success = true,
                unreadCount = unreadCount
            });
        }

        // ============================
        // NOTIFICATIONS: MARK ALL READ (AJAX POST)
        // ============================
        [HttpPost]
        public IActionResult MarkAllNotificationsReadAjax()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return Json(new { success = false, message = "Not authenticated." });
            }

            var unread = _context.Notifications.Where(n => n.UserId == userId.Value && !n.IsRead).ToList();
            foreach (var n in unread)
            {
                n.IsRead = true;
            }

            _context.SaveChanges();

            return Json(new
            {
                success = true,
                unreadCount = 0
            });
        }

        // ============================
        // NOTIFICATIONS: GET LATEST (AJAX GET)
        // ============================
        [HttpGet]
        public IActionResult GetNotificationsAjax()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return Json(new { unreadCount = 0, notifications = new object[] { } });
            }

            var list = _context.Notifications
                .Where(n => n.UserId == userId.Value)
                .OrderByDescending(n => n.CreatedAt)
                .Take(8)
                .Select(n => new
                {
                    id = n.Id,
                    message = n.Message,
                    isRead = n.IsRead,
                    createdAtFormatted = n.CreatedAt.ToString("MMM dd, hh:mm tt")
                })
                .ToList();

            var unreadCount = _context.Notifications.Count(n => n.UserId == userId.Value && !n.IsRead);

            return Json(new
            {
                unreadCount = unreadCount,
                notifications = list
            });
        }
    }
}
