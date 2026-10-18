using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OnlineFoodOrdering.Data;
using OnlineFoodOrdering.Models;
using System.Linq;

namespace OnlineFoodOrdering.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AccountController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Register GET
        [HttpGet]
        public IActionResult Register()
        {
            if (HttpContext.Session.GetInt32("UserId") != null)
            {
                return RedirectToAction("Index", "Main");
            }
            return View();
        }

        // Register POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Register(User user)
        {
            if (ModelState.IsValid)
            {
                var emailNormalized = user.Email?.Trim().ToLower();
                var existing = _context.Users.FirstOrDefault(u => u.Email.ToLower() == emailNormalized);
                if (existing != null)
                {
                    ModelState.AddModelError("Email", "An account with this email address already exists.");
                    return View(user);
                }

                user.Email = emailNormalized ?? user.Email;
                user.IsAdmin = false; // New registrations are customers by default

                _context.Users.Add(user);
                _context.SaveChanges();

                TempData["Success"] = "Account created successfully! Please sign in with your credentials.";
                return RedirectToAction("Login");
            }
            return View(user);
        }

        // Login GET
        [HttpGet]
        public IActionResult Login()
        {
            if (HttpContext.Session.GetInt32("UserId") != null)
            {
                return RedirectToAction("Index", "Main");
            }
            return View();
        }

        // Login POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(string email, string password)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                TempData["Error"] = "Please enter both email and password.";
                return View();
            }

            var emailNormalized = email.Trim().ToLower();
            var user = _context.Users.FirstOrDefault(u => u.Email.ToLower() == emailNormalized && u.Password == password);

            if (user != null)
            {
                // Authenticate session
                HttpContext.Session.SetInt32("UserId", user.UserId);
                HttpContext.Session.SetString("UserName", user.FullName);
                HttpContext.Session.SetInt32("IsAdmin", user.IsAdmin ? 1 : 0);

                TempData["Success"] = $"Welcome back, {user.FullName}!";

                if (user.IsAdmin)
                {
                    return RedirectToAction("Index", "Admin");
                }

                return RedirectToAction("Index", "Main");
            }

            TempData["Error"] = "Invalid email or password. Please verify and try again.";
            return View();
        }

        // Logout
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            TempData["Success"] = "You have been signed out safely.";
            return RedirectToAction("Index", "Main");
        }
    }
}
