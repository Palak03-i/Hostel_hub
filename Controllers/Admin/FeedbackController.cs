using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Hostel_hub.Models;
using Hostel_hub.Services;

namespace Hostel_hub.Controllers.Admin
{
    [Authorize(Roles = "Admin")]
    [Route("Admin/Feedback")]
    public class FeedbackController : Controller
    {
        private readonly IFeedbackService _feedbackService;

        public FeedbackController(IFeedbackService feedbackService)
        {
            _feedbackService = feedbackService;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index(FeedbackSource? source)
        {
            var feedback = await _feedbackService.GetAllFeedbackAsync(source);
            ViewBag.SelectedSource = source;
            return View(feedback);
        }
    }
}