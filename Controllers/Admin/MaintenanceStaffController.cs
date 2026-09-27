using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Hostel_hub.Services;
using Hostel_hub.ViewModels;

namespace Hostel_hub.Controllers.Admin
{
    [Authorize(Roles = "Admin")]
    [Route("Admin/MaintenanceStaff")]
    public class MaintenanceStaffController : Controller
    {
        private readonly IMaintenanceStaffService _staffService;

        public MaintenanceStaffController(IMaintenanceStaffService staffService)
        {
            _staffService = staffService;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index(string? search)
        {
            return View(await _staffService.GetAllAsync(search));
        }

        [HttpGet("Create")]
        public IActionResult Create() => View();

        [HttpPost("Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MaintenanceStaffCreateViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var (success, error) = await _staffService.CreateAsync(model.Email, model.Password, model.FullName, model.PhoneNumber, model.Specialization);
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

            return View(new MaintenanceStaffEditViewModel { FullName = staff.FullName, PhoneNumber = staff.PhoneNumber, Specialization = staff.Specialization });
        }

        [HttpPost("Edit/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, MaintenanceStaffEditViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            bool success = await _staffService.UpdateAsync(id, model.FullName, model.PhoneNumber, model.Specialization);
            if (!success) return NotFound();

            return RedirectToAction("Index");
        }
    }
}