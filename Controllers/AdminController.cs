using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineFoodOrdering.Data;
using OnlineFoodOrdering.Models;
using System.Linq;

namespace OnlineFoodOrdering.Controllers
{
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }
        private bool IsAdmin() => HttpContext.Session.GetInt32("IsAdmin") == 1;

        // ================= DASHBOARD =================
        public IActionResult Index()
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Main");

            return View();
        }

        // ================= VIEW ORDERS =================
        public IActionResult Orders(string status, string sortOrder = "desc")
        {
            // Only admin can access
            if (!IsAdmin())
                return RedirectToAction("Index", "Main");

            // Fetch orders with related user and order items
            var orders = _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.FoodItem)
                .AsQueryable();

            // Filter by status if provided
            if (!string.IsNullOrEmpty(status))
            {
                orders = orders.Where(o => o.Status == status);
                ViewData["SelectedStatus"] = status;
            }

            // Sort by OrderId: descending by default, ascending if requested
            orders = sortOrder == "asc"
                ? orders.OrderBy(o => o.OrderId)
                : orders.OrderByDescending(o => o.OrderId);

            ViewData["SortOrder"] = sortOrder;

            return View(orders.ToList());
        }

        // ================= UPDATE ORDER STATUS =================
        [HttpPost]
        public IActionResult UpdateOrderStatus(int id, string status)
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Main");

            var order = _context.Orders.FirstOrDefault(o => o.OrderId == id);
            if (order == null) return NotFound();

            order.Status = status;

            // Create user notification
            _context.Notifications.Add(new Notification
            {
                UserId = order.UserId,
                Message = $"Your order #{order.OrderId} is {status}",
                IsRead = false
            });

            _context.SaveChanges();

            return RedirectToAction("Orders");
        }

        // ================= FOOD LIST =================
        public IActionResult FoodList()
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Main");

            var items = _context.FoodItems.ToList();
            return View(items);
        }

        // ================= ADD FOOD =================
        public IActionResult AddFood()
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Main");

            return View();
        }

        [HttpPost]
        public IActionResult AddFood(FoodItem item, IFormFile image)
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Main");

            if (image != null)
            {
                var fileName = System.IO.Path.GetFileName(image.FileName);
                var path = $"wwwroot/images/{fileName}";

                using (var stream = System.IO.File.Create(path))
                    image.CopyTo(stream);

                item.ImagePath = $"/images/{fileName}";
            }

            _context.FoodItems.Add(item);
            _context.SaveChanges();

            TempData["Success"] = "Food item added successfully!";
            return RedirectToAction("FoodList");
        }

        // ================= EDIT FOOD =================
        public IActionResult EditFood(int id)
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Main");

            var item = _context.FoodItems.FirstOrDefault(f => f.FoodItemId == id);
            if (item == null) return NotFound();

            return View(item);
        }

        [HttpPost]
        public IActionResult EditFood(FoodItem item, IFormFile image)
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Main");

            var existing = _context.FoodItems.FirstOrDefault(f => f.FoodItemId == item.FoodItemId);
            if (existing == null) return NotFound();

            existing.Name = item.Name;
            existing.Price = item.Price;

            if (image != null)
            {
                var fileName = System.IO.Path.GetFileName(image.FileName);
                var path = $"wwwroot/images/{fileName}";

                using (var stream = System.IO.File.Create(path))
                    image.CopyTo(stream);

                existing.ImagePath = $"/images/{fileName}";
            }

            _context.SaveChanges();
            TempData["Success"] = "Food item updated successfully!";
            return RedirectToAction("FoodList");
        }

        // ================= DELETE FOOD =================
        public IActionResult DeleteFood(int id)
        {
            if (!IsAdmin())
                return RedirectToAction("Index", "Main");

            var item = _context.FoodItems.FirstOrDefault(f => f.FoodItemId == id);
            if (item != null)
            {
                _context.FoodItems.Remove(item);
                _context.SaveChanges();
            }

            return RedirectToAction("FoodList");
        }
    }
}
