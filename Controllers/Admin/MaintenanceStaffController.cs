using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Hostel_hub.Services;
using Hostel_hub.ViewModels;

namespace Hostel_hub.Controllers.Admin
{
    [Authorize(Roles = "Admin")]
    [Route("Admin/MaintenanceStaff")]
    public class MaintenanceStaffController : Controller
    {
        private readonly IMaintenanceStaffService _staffService;
        private readonly IWardenContext _wardenContext;
        private readonly IWardenService _wardenService;

        public MaintenanceStaffController(IMaintenanceStaffService staffService, IWardenContext wardenContext, IWardenService wardenService)
        {
            _staffService = staffService;
            _wardenContext = wardenContext;
            _wardenService = wardenService;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index(string? search)
        {
            var userId = GetCurrentUserId();

            var scopedHostelId =
                await _wardenContext.GetScopedHostelIdAsync(userId);

            bool isSuperAdmin = !scopedHostelId.HasValue;

            var model = new StaffManagementViewModel
            {
                IsSuperAdmin = isSuperAdmin,

                MaintenanceStaff =
                    await _staffService.GetAllAsync(
                        search,
                        scopedHostelId)
            };

            if (isSuperAdmin)
            {
                model.Wardens =
                    await _wardenService.GetAllAsync();
            }

            return View(model);
        }

        [HttpGet("Create")]
        public async Task<IActionResult> Create()
        {
            var scopedHostelId = await _wardenContext.GetScopedHostelIdAsync(GetCurrentUserId());
            if (!scopedHostelId.HasValue)
            {
                TempData["ErrorMessage"] = "Only a Warden can create staff for their own hostel.";
                return RedirectToAction("Index");
            }

            return View();
        }

        [HttpPost("Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MaintenanceStaffCreateViewModel model)
        {
            var scopedHostelId = await _wardenContext.GetScopedHostelIdAsync(GetCurrentUserId());
            if (!scopedHostelId.HasValue)
            {
                TempData["ErrorMessage"] = "Only a Warden can create staff for their own hostel.";
                return RedirectToAction("Index");
            }

            if (!ModelState.IsValid) return View(model);

            var (success, error) = await _staffService.CreateAsync(model.Email, model.Password, model.FullName, model.PhoneNumber, model.Specialization, scopedHostelId.Value);
            if (!success)
            {
                ModelState.AddModelError(string.Empty, error!);
                return View(model);
            }
            return RedirectToAction("Index");
        }

        [HttpGet("Edit/{id}")]
        public async Task<IActionResult> Edit(int id)
        {
            var staff = await _staffService.GetByIdAsync(id);
            if (staff == null) return NotFound();

            if (!await CanManageAsync(staff.HostelId))
            {
                return Forbid();
            }

            return View(new MaintenanceStaffEditViewModel { FullName = staff.FullName, PhoneNumber = staff.PhoneNumber, Specialization = staff.Specialization });
        }

        [HttpPost("Edit/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, MaintenanceStaffEditViewModel model)
        {
            var staff = await _staffService.GetByIdAsync(id);
            if (staff == null) return NotFound();

            if (!await CanManageAsync(staff.HostelId))
            {
                return Forbid();
            }

            if (!ModelState.IsValid) return View(model);

            bool success = await _staffService.UpdateAsync(id, model.FullName, model.PhoneNumber, model.Specialization);
            if (!success) return NotFound();

            return RedirectToAction("Index");
        }

        // Unlike CanAccessHostelAsync elsewhere, this does NOT let Super Admin
        // bypass — only the Warden who actually owns this staff member's
        // hostel may manage them. A staff member with no HostelId assigned
        // yet (null) is unmanageable by anyone until one is set.
        private async Task<bool> CanManageAsync(int? staffHostelId)
        {
            if (!staffHostelId.HasValue) return false;

            var scopedHostelId = await _wardenContext.GetScopedHostelIdAsync(GetCurrentUserId());
            return scopedHostelId.HasValue && scopedHostelId.Value == staffHostelId.Value;
        }

        private int GetCurrentUserId()
        {
            string? userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.Parse(userIdClaim!);
        }
    }
}