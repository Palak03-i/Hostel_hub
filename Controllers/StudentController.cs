using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Hostel_hub.Services;
using Hostel_hub.ViewModels;

namespace Hostel_hub.Controllers
{
    [Authorize(Roles = "Student")]
    public class StudentController : Controller
    {
        private readonly IStudentService _studentService;

        public StudentController(IStudentService studentService)
        {
            _studentService = studentService;
        }

        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            int userId = GetCurrentUserId();
            var student = await _studentService.GetProfileByUserIdAsync(userId);

            if (student == null)
            {
                return NotFound();
            }

            var viewModel = new StudentProfileViewModel
            {
                FullName = student.FullName,
                RollNumber = student.RollNumber,
                PhoneNumber = student.PhoneNumber,
                HostelName = student.Hostel?.Name,
                RoomNumber = student.Room?.RoomNumber
            };

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> EditProfile()
        {
            int userId = GetCurrentUserId();
            var student = await _studentService.GetProfileByUserIdAsync(userId);

            if (student == null)
            {
                return NotFound();
            }

            var viewModel = new StudentEditProfileViewModel
            {
                FullName = student.FullName,
                PhoneNumber = student.PhoneNumber
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfile(StudentEditProfileViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            int userId = GetCurrentUserId();
            bool success = await _studentService.UpdateProfileAsync(userId, model.FullName, model.PhoneNumber);

            if (!success)
            {
                return NotFound();
            }

            return RedirectToAction("Profile");
        }

        private int GetCurrentUserId()
        {
            string? userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.Parse(userIdClaim!);
        }
    }
}