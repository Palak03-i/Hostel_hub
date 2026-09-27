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
        private readonly IRoomChangeService _roomChangeService;
        private readonly IRoomService _roomService;

        public StudentController(IStudentService studentService, IRoomChangeService roomChangeService, IRoomService roomService)
        {
            _studentService = studentService;
            _roomChangeService = roomChangeService;
            _roomService = roomService;
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
        [HttpGet]
        public async Task<IActionResult> RoomChangeRequests()
        {
            var student = await _studentService.GetProfileByUserIdAsync(GetCurrentUserId());
            if (student == null)
            {
                return NotFound();
            }

            var requests = await _roomChangeService.GetRequestsForStudentAsync(student.StudentId);
            return View(requests);
        }

        [HttpGet]
        public async Task<IActionResult> RequestRoomChange()
        {
            var student = await _studentService.GetProfileByUserIdAsync(GetCurrentUserId());
            if (student == null || !student.RoomId.HasValue)
            {
                TempData["ErrorMessage"] = "You must have an existing room before requesting a change.";
                return RedirectToAction("Profile");
            }

            var allRooms = await _roomService.GetRoomsByHostelAsync(null);
            ViewBag.AvailableRooms = allRooms.Where(r => r.RoomId != student.RoomId.Value).ToList();

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestRoomChange(RoomChangeCreateViewModel model)
        {
            var student = await _studentService.GetProfileByUserIdAsync(GetCurrentUserId());
            if (student == null)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                var allRooms = await _roomService.GetRoomsByHostelAsync(null);
                ViewBag.AvailableRooms = allRooms.Where(r => r.RoomId != student.RoomId).ToList();
                return View(model);
            }

            var (success, errorMessage) = await _roomChangeService.CreateRequestAsync(student.StudentId, model.RequestedRoomId, model.Reason);
            if (!success)
            {
                ModelState.AddModelError(string.Empty, errorMessage!);
                var allRooms = await _roomService.GetRoomsByHostelAsync(null);
                ViewBag.AvailableRooms = allRooms.Where(r => r.RoomId != student.RoomId).ToList();
                return View(model);
            }

            return RedirectToAction("RoomChangeRequests");
        }
    }
}