using Hostel_hub.Models;
using Hostel_hub.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using static Hostel_hub.Models.Complaint;

namespace Hostel_hub.Controllers.Admin
{
    [Authorize(Roles = "Admin")]
    [Route("Admin/Complaints")]
    public class ComplaintsController : Controller
    {
        private readonly IComplaintService _complaintService;
        private readonly IMaintenanceStaffService _staffService;
        private readonly IWardenContext _wardenContext;

        public ComplaintsController(IComplaintService complaintService, IMaintenanceStaffService staffService, IWardenContext wardenContext)
        {
            _complaintService = complaintService;
            _staffService = staffService;
            _wardenContext = wardenContext;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index(ComplaintStatus? status, ComplaintCategory? category, ComplaintPriority? priority)
        {
            var scopedHostelId = await _wardenContext.GetScopedHostelIdAsync(GetCurrentUserId());
            var complaints = await _complaintService.GetAllComplaintsAsync(scopedHostelId, status, category, priority);
            return View(complaints);
        }

        [HttpGet("Details/{id}")]
        public async Task<IActionResult> Details(int id)
        {
            var complaint = await _complaintService.GetComplaintByIdAsync(id);
            if (complaint == null) return NotFound();

            ViewBag.History = await _complaintService.GetHistoryAsync(id);
            ViewBag.StaffList = await _staffService.GetAllAsync(null);
            return View(complaint);
        }

        [HttpPost("SetPriority/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetPriority(int id, ComplaintPriority priority)
        {
            var (success, error) = await _complaintService.SetPriorityAsync(id, priority, "Admin");
            if (!success) TempData["ErrorMessage"] = error;
            return RedirectToAction("Details", new { id });
        }

        [HttpPost("Assign/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Assign(int id, int staffId)
        {
            var (success, error) = await _complaintService.AssignStaffAsync(id, staffId, "Admin");
            if (!success) TempData["ErrorMessage"] = error;
            return RedirectToAction("Details", new { id });
        }

        private int GetCurrentUserId()
        {
            string? userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.Parse(userIdClaim!);
        }
    }
}