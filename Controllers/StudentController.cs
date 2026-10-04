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
        private readonly IComplaintService _complaintService;
        private readonly IMessMenuService _messMenuService;
        private readonly IMealSelectionService _mealSelectionService;
        private readonly IFeedbackService _feedbackService;
        private readonly IAnnouncementService _announcementService;
        public StudentController(IStudentService studentService, IRoomChangeService roomChangeService, IRoomService roomService, IComplaintService complaintService, IMessMenuService messMenuService, IMealSelectionService mealSelectionService, IFeedbackService feedbackService, IAnnouncementService announcementService)
        {
            _studentService = studentService;
            _roomChangeService = roomChangeService;
            _roomService = roomService;
            _complaintService = complaintService;
            _messMenuService = messMenuService;
            _mealSelectionService = mealSelectionService;
            _feedbackService = feedbackService;
            _announcementService = announcementService;
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
        [HttpGet]
        public async Task<IActionResult> Complaints()
        {
            var student = await _studentService.GetProfileByUserIdAsync(GetCurrentUserId());
            if (student == null) return NotFound();

            var complaints = await _complaintService.GetComplaintsForStudentAsync(student.StudentId);
            return View(complaints);
        }

        [HttpGet]
        public IActionResult CreateComplaint() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateComplaint(ComplaintCreateViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var student = await _studentService.GetProfileByUserIdAsync(GetCurrentUserId());
            if (student == null) return NotFound();

            var (success, error) = await _complaintService.CreateComplaintAsync(student.StudentId, model.Title, model.Description, model.Category);
            if (!success)
            {
                ModelState.AddModelError(string.Empty, error!);
                return View(model);
            }

            return RedirectToAction("Complaints");
        }

        [HttpGet]
        public async Task<IActionResult> ComplaintDetails(int id)
        {
            var student = await _studentService.GetProfileByUserIdAsync(GetCurrentUserId());
            if (student == null) return NotFound();

            var complaint = await _complaintService.GetComplaintByIdAsync(id);
            if (complaint == null || complaint.StudentId != student.StudentId)
            {
                return Forbid();
            }

            ViewBag.History = await _complaintService.GetHistoryAsync(id);
            return View(complaint);
        }
        [HttpGet]
        public async Task<IActionResult> Mess()
        {
            int userId = GetCurrentUserId();
            var student = await _studentService.GetProfileByUserIdAsync(userId);

            if (student == null)
            {
                return NotFound();
            }

            var model = new MessOverviewViewModel();

            if (student.HostelId == null)
            {
                model.NoticeMessage = "You are not currently allocated to a hostel, so no mess menu is available yet.";
                return View(model);
            }

            var today = DateOnly.FromDateTime(DateTime.Today);
            var tomorrow = today.AddDays(1);

            model.TodayMenu = await _messMenuService.GetMenuByHostelAndDateAsync(student.HostelId.Value, today);
            model.TomorrowMenu = await _messMenuService.GetMenuByHostelAndDateAsync(student.HostelId.Value, tomorrow);

            if (model.TodayMenu == null && model.TomorrowMenu == null)
            {
                model.NoticeMessage = "No menu has been published for your hostel yet.";
            }

            if (model.TodayMenu != null)
            {
                var todaySelections = await _mealSelectionService.GetSelectionsForStudentAsync(student.StudentId, model.TodayMenu.MessMenuId);
                model.TodaySelections = todaySelections.ToDictionary(s => s.MealType, s => s.Status);
            }

            if (model.TomorrowMenu != null)
            {
                var tomorrowSelections = await _mealSelectionService.GetSelectionsForStudentAsync(student.StudentId, model.TomorrowMenu.MessMenuId);
                model.TomorrowSelections = tomorrowSelections.ToDictionary(s => s.MealType, s => s.Status);
            }

            return View(model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SelectMeal(int menuId, Hostel_hub.Models.MealType mealType, Hostel_hub.Models.MealSelectionStatus status)
        {
            int userId = GetCurrentUserId();
            var student = await _studentService.GetProfileByUserIdAsync(userId);

            if (student == null)
            {
                return NotFound();
            }

            var (success, errorMessage) = await _mealSelectionService.SelectMealAsync(student.StudentId, menuId, mealType, status);

            if (!success)
            {
                TempData["ErrorMessage"] = errorMessage;
            }
            else
            {
                TempData["SuccessMessage"] = $"{mealType} set to {status}.";
            }

            return RedirectToAction("Mess");
        }
        [HttpGet]
        public async Task<IActionResult> Feedback()
        {
            var student = await _studentService.GetProfileByUserIdAsync(GetCurrentUserId());
            if (student == null) return NotFound();

            var feedback = await _feedbackService.GetFeedbackForStudentAsync(student.StudentId);
            return View(feedback);
        }

        [HttpGet]
        public async Task<IActionResult> SubmitFeedback()
        {
            var student = await _studentService.GetProfileByUserIdAsync(GetCurrentUserId());
            if (student == null) return NotFound();

            var resolvedComplaints = (await _complaintService.GetComplaintsForStudentAsync(student.StudentId))
                .Where(c => c.Status == Hostel_hub.Models.ComplaintStatus.Resolved)
                .ToList();
            ViewBag.ResolvedComplaints = resolvedComplaints;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitFeedback(FeedbackCreateViewModel model)
        {
            var student = await _studentService.GetProfileByUserIdAsync(GetCurrentUserId());
            if (student == null) return NotFound();

            if (!ModelState.IsValid)
            {
                var resolvedComplaints = (await _complaintService.GetComplaintsForStudentAsync(student.StudentId))
                    .Where(c => c.Status == Hostel_hub.Models.ComplaintStatus.Resolved)
                    .ToList();
                ViewBag.ResolvedComplaints = resolvedComplaints;
                return View(model);
            }

            var (success, errorMessage) = await _feedbackService.SubmitFeedbackAsync(student.StudentId, model.Source, model.ComplaintId, model.Rating, model.Comments);
            if (!success)
            {
                ModelState.AddModelError(string.Empty, errorMessage!);
                var resolvedComplaints = (await _complaintService.GetComplaintsForStudentAsync(student.StudentId))
                    .Where(c => c.Status == Hostel_hub.Models.ComplaintStatus.Resolved)
                    .ToList();
                ViewBag.ResolvedComplaints = resolvedComplaints;
                return View(model);
            }

            return RedirectToAction("Feedback");
        }
        [HttpGet]
        public async Task<IActionResult> Announcements()
        {
            var student = await _studentService.GetProfileByUserIdAsync(GetCurrentUserId());
            if (student == null) return NotFound();

            var announcements = await _announcementService.GetActiveAnnouncementsAsync(student.HostelId);
            return View(announcements);
        }
        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            var student = await _studentService.GetProfileByUserIdAsync(GetCurrentUserId());
            if (student == null) return NotFound();

            var model = new StudentDashboardViewModel
            {
                FullName = student.FullName,
                HostelName = student.Hostel?.Name,
                RoomNumber = student.Room?.RoomNumber,
                RoomCapacity = student.Room?.Capacity,
                RoomCurrentOccupancy = student.Room?.CurrentOccupancy
            };

            if (student.HostelId.HasValue)
            {
                var today = DateOnly.FromDateTime(DateTime.Today);
                model.TodayMenu = await _messMenuService.GetMenuByHostelAndDateAsync(student.HostelId.Value, today);

                if (model.TodayMenu != null)
                {
                    var selections = await _mealSelectionService.GetSelectionsForStudentAsync(student.StudentId, model.TodayMenu.MessMenuId);
                    model.TodaySelections = selections.ToDictionary(s => s.MealType, s => s.Status);
                }
            }

            var allComplaints = await _complaintService.GetComplaintsForStudentAsync(student.StudentId);
            model.ActiveComplaints = allComplaints
                .Where(c => c.Status != Hostel_hub.Models.ComplaintStatus.Resolved)
                .ToList();
            model.LatestComplaint = allComplaints
                .OrderByDescending(c => c.CreatedAt)
                .FirstOrDefault();

            model.RecentAnnouncements = (await _announcementService.GetActiveAnnouncementsAsync(student.HostelId))
                .Take(5)
                .ToList();

            var roomChangeRequests = await _roomChangeService.GetRequestsForStudentAsync(student.StudentId);
            model.PendingRoomChangeRequest = roomChangeRequests
                .FirstOrDefault(r => r.Status == Hostel_hub.Models.RoomChangeStatus.Pending);

            return View(model);
        }

    }
}