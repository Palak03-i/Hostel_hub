using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Hostel_hub.Models;
using Hostel_hub.Services;
using Hostel_hub.ViewModels;

namespace Hostel_hub.Controllers.Admin
{
    [Authorize(Roles = "Admin")]
    [Route("Admin/Announcements")]
    public class AnnouncementsController : Controller
    {
        private readonly IAnnouncementService _announcementService;

        public AnnouncementsController(IAnnouncementService announcementService)
        {
            _announcementService = announcementService;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            var announcements = await _announcementService.GetAllAnnouncementsAsync();
            return View(announcements);
        }

        [HttpGet("Create")]
        public IActionResult Create() => View();

        [HttpPost("Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AnnouncementViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var announcement = new Announcement
            {
                Title = model.Title,
                Content = model.Content,
                PublishDate = model.PublishDate,
                ExpiryDate = model.ExpiryDate,
                IsImportant = model.IsImportant,
                PostedByUserId = GetCurrentUserId()
            };

            var (success, errorMessage) = await _announcementService.CreateAnnouncementAsync(announcement);
            if (!success)
            {
                ModelState.AddModelError(string.Empty, errorMessage!);
                return View(model);
            }

            return RedirectToAction("Index");
        }

        [HttpGet("Edit/{id}")]
        public async Task<IActionResult> Edit(int id)
        {
            var announcement = await _announcementService.GetByIdAsync(id);
            if (announcement == null) return NotFound();

            var model = new AnnouncementViewModel
            {
                Title = announcement.Title,
                Content = announcement.Content,
                PublishDate = announcement.PublishDate,
                ExpiryDate = announcement.ExpiryDate,
                IsImportant = announcement.IsImportant
            };

            ViewBag.AnnouncementId = id;
            return View(model);
        }

        [HttpPost("Edit/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, AnnouncementViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.AnnouncementId = id;
                return View(model);
            }

            var updated = new Announcement
            {
                Title = model.Title,
                Content = model.Content,
                PublishDate = model.PublishDate,
                ExpiryDate = model.ExpiryDate,
                IsImportant = model.IsImportant
            };

            var (success, errorMessage) = await _announcementService.UpdateAnnouncementAsync(id, updated);
            if (!success)
            {
                ModelState.AddModelError(string.Empty, errorMessage!);
                ViewBag.AnnouncementId = id;
                return View(model);
            }

            return RedirectToAction("Index");
        }

        [HttpPost("Deactivate/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Deactivate(int id)
        {
            var (success, errorMessage) = await _announcementService.DeactivateAnnouncementAsync(id);
            if (!success)
            {
                TempData["ErrorMessage"] = errorMessage;
            }

            return RedirectToAction("Index");
        }

        private int GetCurrentUserId()
        {
            string? userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.Parse(userIdClaim!);
        }
    }
}