using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Hostel_hub.Data;
using Hostel_hub.Models;
using Hostel_hub.Services;
using Hostel_hub.ViewModels;

namespace Hostel_hub.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuthService _authService;

        public AccountController(ApplicationDbContext context, IAuthService authService)
        {
            _context = context;
            _authService = authService;
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            bool emailExists = await _context.Users.AnyAsync(u => u.Email == model.Email);
            if (emailExists)
            {
                ModelState.AddModelError(nameof(model.Email), "This email is already registered.");
                return View(model);
            }

            bool rollNumberExists = await _context.Students.AnyAsync(s => s.RollNumber == model.RollNumber);
            if (rollNumberExists)
            {
                ModelState.AddModelError(nameof(model.RollNumber), "This roll number is already registered.");
                return View(model);
            }

            var user = new User
            {
                Email = model.Email,
                PasswordHash = _authService.HashPassword(model.Password),
                Role = UserRole.Student
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var student = new Student
            {
                UserId = user.UserId,
                FullName = model.FullName,
                RollNumber = model.RollNumber
            };
            _context.Students.Add(student);
            await _context.SaveChangesAsync();

            await _authService.SignInAsync(HttpContext, user.UserId, user.Email, user.Role.ToString());

            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            if (!ModelState.IsValid)
            {
                ViewData["ReturnUrl"] = returnUrl;
                return View(model);
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.Email);

            if (user == null || !_authService.VerifyPassword(model.Password, user.PasswordHash))
            {
                ModelState.AddModelError(string.Empty, "Invalid email or password.");
                ViewData["ReturnUrl"] = returnUrl;
                return View(model);
            }

            await _authService.SignInAsync(HttpContext, user.UserId, user.Email, user.Role.ToString());

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _authService.SignOutAsync(HttpContext);
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}