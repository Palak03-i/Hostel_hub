using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Hostel_hub.Services;
using System.Security.Claims;

namespace Hostel_hub.Controllers.Admin
{
    [Authorize(Roles = "Admin")]
    [Route("Admin/Students")]
    public class StudentsController : Controller
    {
        private readonly IStudentService _studentService;
        private readonly IWardenContext _wardenContext;

        public StudentsController(IStudentService studentService, IWardenContext wardenContext)
        {
            _studentService = studentService;
            _wardenContext = wardenContext;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index(string? search, int? hostelId)
        {
            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdValue, out int userId))
            {
                return Unauthorized();
            }

            var scopedHostelId =
                await _wardenContext.GetScopedHostelIdAsync(userId);

            int? effectiveHostelId;

            if (scopedHostelId.HasValue)
            {
                // Warden: always force their own hostel
                effectiveHostelId = scopedHostelId.Value;
            }
            else
            {
                // Super Admin: may view all or filter by hostel
                effectiveHostelId = hostelId;
            }

            var students =
                await _studentService.GetAllStudentsAsync(
                    search,
                    effectiveHostelId);

            return View(students);
        }

        [HttpGet("Details/{id}")]
        public async Task<IActionResult> Details(int id)
        {
            var student = await _studentService.GetStudentByIdAsync(id);

            if (student == null)
            {
                return NotFound();
            }

            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdValue, out int userId))
            {
                return Unauthorized();
            }

            var scopedHostelId =
                await _wardenContext.GetScopedHostelIdAsync(userId);

            if (scopedHostelId.HasValue &&
                student.HostelId != scopedHostelId.Value)
            {
                return Forbid();
            }

            return View(student);
        }
    }
}