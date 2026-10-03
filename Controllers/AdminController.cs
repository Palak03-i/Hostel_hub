using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Hostel_hub.Services;

namespace Hostel_hub.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly IAdminDashboardService _dashboardService;
        private readonly IWardenContext _wardenContext;

        public AdminController(IAdminDashboardService dashboardService, IWardenContext wardenContext)
        {
            _dashboardService = dashboardService;
            _wardenContext = wardenContext;
        }

        public async Task<IActionResult> Dashboard()
        {
            int userId = GetCurrentUserId();
            var scopedHostelId = await _wardenContext.GetScopedHostelIdAsync(userId);
            var model = await _dashboardService.GetDashboardAsync(scopedHostelId);
            return View(model);
        }

        private int GetCurrentUserId()
        {
            string? userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.Parse(userIdClaim!);
        }
    }
}