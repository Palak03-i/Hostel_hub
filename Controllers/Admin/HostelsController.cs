using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Hostel_hub.Models;
using Hostel_hub.Services;
using Hostel_hub.ViewModels;

namespace Hostel_hub.Controllers.Admin
{
    [Authorize(Roles = "Admin")]
    [Route("Admin/Hostels")]
    public class HostelsController : Controller
    {
        private readonly IHostelService _hostelService;
        private readonly IWardenContext _wardenContext;

        public HostelsController(IHostelService hostelService, IWardenContext wardenContext)
        {
            _hostelService = hostelService;
            _wardenContext = wardenContext;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            int userId = GetCurrentUserId();
            var scopedHostelId = await _wardenContext.GetScopedHostelIdAsync(userId);

            var hostels = await _hostelService.GetAllHostelsAsync();

            if (scopedHostelId.HasValue)
            {
                hostels = hostels.Where(h => h.HostelId == scopedHostelId.Value).ToList();
            }

            return View(hostels);
        }

        [HttpGet("Details/{id}")]
        public async Task<IActionResult> Details(int id)
        {
            if (!await CanAccessHostelAsync(id))
            {
                return Forbid();
            }

            var hostel = await _hostelService.GetHostelByIdAsync(id);
            if (hostel == null)
            {
                return NotFound();
            }
            return View(hostel);
        }

        [HttpGet("Create")]
        public async Task<IActionResult> Create()
        {
            if (!await _wardenContext.IsSuperAdminAsync(GetCurrentUserId()))
            {
                return Forbid();
            }
            return View();
        }

        [HttpPost("Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(HostelViewModel model)
        {
            if (!await _wardenContext.IsSuperAdminAsync(GetCurrentUserId()))
            {
                return Forbid();
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var hostel = new Hostel { Name = model.Name, Type = model.Type };
            await _hostelService.CreateHostelAsync(hostel);
            return RedirectToAction("Index");
        }

        [HttpGet("Edit/{id}")]
        public async Task<IActionResult> Edit(int id)
        {
            if (!await CanAccessHostelAsync(id))
            {
                return Forbid();
            }

            var hostel = await _hostelService.GetHostelByIdAsync(id);
            if (hostel == null)
            {
                return NotFound();
            }

            var model = new HostelViewModel { Name = hostel.Name, Type = hostel.Type };
            return View(model);
        }

        [HttpPost("Edit/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, HostelViewModel model)
        {
            if (!await CanAccessHostelAsync(id))
            {
                return Forbid();
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            bool success = await _hostelService.UpdateHostelAsync(id, model.Name, model.Type);
            if (!success)
            {
                return NotFound();
            }

            return RedirectToAction("Index");
        }

        [HttpPost("Delete/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            if (!await _wardenContext.IsSuperAdminAsync(GetCurrentUserId()))
            {
                return Forbid();
            }

            var (success, errorMessage) = await _hostelService.DeleteHostelAsync(id);
            if (!success)
            {
                TempData["ErrorMessage"] = errorMessage;
            }

            return RedirectToAction("Index");
        }

        private async Task<bool> CanAccessHostelAsync(int hostelId)
        {
            int userId = GetCurrentUserId();
            var scopedHostelId = await _wardenContext.GetScopedHostelIdAsync(userId);

            return !scopedHostelId.HasValue || scopedHostelId.Value == hostelId;
        }

        private int GetCurrentUserId()
        {
            string? userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.Parse(userIdClaim!);
        }
    }
}