using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineFoodOrdering.Data;
using OnlineFoodOrdering.Models;
using System;
using System.IO;
using System.Linq;

namespace OnlineFoodOrdering.Controllers
{
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public AdminController(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // 🔐 Admin check helper
        private bool IsAdmin() => HttpContext.Session.GetInt32("IsAdmin") == 1;

        // ================= DASHBOARD =================
        public IActionResult Index()
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Main");

            var totalOrders = _context.Orders.Count();
            var processingOrders = _context.Orders.Count(o => o.Status == "Processing");
            var confirmedOrders = _context.Orders.Count(o => o.Status == "Confirmed");
            var deliveredOrders = _context.Orders.Count(o => o.Status == "Delivered");
            var totalFoodItems = _context.FoodItems.Count();

            // Calculate total revenue from order items
            var revenue = _context.OrderItems
                .Include(oi => oi.FoodItem)
                .Sum(oi => (decimal?)((oi.FoodItem != null ? oi.FoodItem.Price : 0) * oi.Quantity)) ?? 0;

            ViewBag.TotalOrders = totalOrders;
            ViewBag.ProcessingOrders = processingOrders;
            ViewBag.ConfirmedOrders = confirmedOrders;
            ViewBag.DeliveredOrders = deliveredOrders;
            ViewBag.TotalFoodItems = totalFoodItems;
            ViewBag.TotalRevenue = revenue;

            // Fetch recent 5 orders for dashboard quick view
            var recentOrders = _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.FoodItem)
                .OrderByDescending(o => o.OrderId)
                .Take(5)
                .ToList();

            return View(recentOrders);
        }

        // ================= VIEW ORDERS =================
        public IActionResult Orders(string status, string sortOrder = "desc")
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Main");

            var orders = _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.FoodItem)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status))
            {
                orders = orders.Where(o => o.Status == status);
                ViewData["SelectedStatus"] = status;
            }

            orders = sortOrder == "asc"
                ? orders.OrderBy(o => o.OrderId)
                : orders.OrderByDescending(o => o.OrderId);

            ViewData["SortOrder"] = sortOrder;

            return View(orders.ToList());
        }

        // ================= UPDATE ORDER STATUS =================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateOrderStatus(int id, string status)
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Main");

            var order = _context.Orders.FirstOrDefault(o => o.OrderId == id);
            if (order == null) return NotFound();

            order.Status = status;

            // Create user notification for order status change
            _context.Notifications.Add(new Notification
            {
                UserId = order.UserId,
                Message = $"Your order #{order.OrderId} status has been updated to: {status}",
                IsRead = false,
                CreatedAt = DateTime.Now
            });

            _context.SaveChanges();

            TempData["Success"] = $"Order #{order.OrderId} status updated to {status}!";
            return RedirectToAction("Orders");
        }

        // ================= FOOD LIST =================
        public IActionResult FoodList()
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Main");

            var items = _context.FoodItems.OrderBy(f => f.Name).ToList();
            return View(items);
        }

        // ================= ADD FOOD =================
        [HttpGet]
        public IActionResult AddFood()
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Main");

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddFood(FoodItem item, IFormFile? image)
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Main");

            // Server-side enforcement: Publishing new food items is disabled in Demo Mode
            bool isDemoMode = _configuration.GetValue<bool?>("DemoMode") ?? true;
            if (isDemoMode)
            {
                ModelState.AddModelError(string.Empty, "Demo Mode: Publishing new food items is disabled to prevent unauthorized changes to the demonstration database.");
                TempData["Error"] = "Demo Mode: Publishing new food items is disabled to prevent unauthorized changes to the demonstration database.";
                return View(item);
            }

            if (image != null && image.Length > 0)
            {
                var originalName = Path.GetFileName(image.FileName);
                var safeFileName = Guid.NewGuid().ToString("N").Substring(0, 8) + "_" + originalName;
                var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images");
                
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                var filePath = Path.Combine(folderPath, safeFileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    image.CopyTo(stream);
                }

                item.ImagePath = $"/images/{safeFileName}";
            }
            else if (string.IsNullOrWhiteSpace(item.ImagePath))
            {
                // Fallback default appetizing image
                item.ImagePath = "/images/burger.jpg";
            }

            if (ModelState.IsValid)
            {
                _context.FoodItems.Add(item);
                _context.SaveChanges();

                TempData["Success"] = $"Food item \"{item.Name}\" added to the menu!";
                return RedirectToAction("FoodList");
            }

            return View(item);
        }

        // ================= EDIT FOOD =================
        [HttpGet]
        public IActionResult EditFood(int id)
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Main");

            var item = _context.FoodItems.FirstOrDefault(f => f.FoodItemId == id);
            if (item == null) return NotFound();

            return View(item);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditFood(FoodItem item, IFormFile? image)
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Main");

            var existing = _context.FoodItems.FirstOrDefault(f => f.FoodItemId == item.FoodItemId);
            if (existing == null) return NotFound();

            existing.Name = item.Name;
            existing.Price = item.Price;

            if (image != null && image.Length > 0)
            {
                var originalName = Path.GetFileName(image.FileName);
                var safeFileName = Guid.NewGuid().ToString("N").Substring(0, 8) + "_" + originalName;
                var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images");
                
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                var filePath = Path.Combine(folderPath, safeFileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    image.CopyTo(stream);
                }

                existing.ImagePath = $"/images/{safeFileName}";
            }

            _context.SaveChanges();
            TempData["Success"] = $"Food item \"{existing.Name}\" updated successfully!";
            return RedirectToAction("FoodList");
        }

        // ================= DELETE FOOD =================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteFood(int id)
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Main");

            var item = _context.FoodItems.FirstOrDefault(f => f.FoodItemId == id);
            if (item != null)
            {
                // Safety check: is it in existing orders?
                var isReferencedInOrders = _context.OrderItems.Any(oi => oi.FoodItemId == id);
                if (isReferencedInOrders)
                {
                    TempData["Error"] = $"Cannot delete \"{item.Name}\" because it is referenced in past customer orders. You can edit its details instead.";
                    return RedirectToAction("FoodList");
                }

                _context.FoodItems.Remove(item);
                _context.SaveChanges();
                TempData["Success"] = $"Food item \"{item.Name}\" has been deleted.";
            }

            return RedirectToAction("FoodList");
        }
    }
}
