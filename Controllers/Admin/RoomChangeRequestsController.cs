using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Hostel_hub.Services;

namespace Hostel_hub.Controllers.Admin
{
    [Authorize(Roles = "Admin")]
    [Route("Admin/RoomChangeRequests")]
    public class RoomChangeRequestsController : Controller
    {
        private readonly IRoomChangeService _roomChangeService;
        private readonly IWardenContext _wardenContext;

        public RoomChangeRequestsController(IRoomChangeService roomChangeService, IWardenContext wardenContext)
        {
            _roomChangeService = roomChangeService;
            _wardenContext = wardenContext;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            var scopedHostelId = await _wardenContext.GetScopedHostelIdAsync(GetCurrentUserId());
            var requests = await _roomChangeService.GetPendingRequestsAsync(scopedHostelId);
            return View(requests);
        }

        [HttpPost("Approve/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            if (!await CanManageRequestAsync(id))
            {
                return Forbid();
            }

            var (success, errorMessage) = await _roomChangeService.ApproveRequestAsync(id);
            if (!success)
            {
                TempData["ErrorMessage"] = errorMessage;
            }
            else
            {
                TempData["SuccessMessage"] = "Room change approved.";
            }

            return RedirectToAction("Index");
        }

        [HttpPost("Reject/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id)
        {
            if (!await CanManageRequestAsync(id))
            {
                return Forbid();
            }

            var (success, errorMessage) = await _roomChangeService.RejectRequestAsync(id);
            if (!success)
            {
                TempData["ErrorMessage"] = errorMessage;
            }
            else
            {
                TempData["SuccessMessage"] = "Room change rejected.";
            }

            return RedirectToAction("Index");
        }

        private async Task<bool> CanManageRequestAsync(int requestId)
        {
            var request = await _roomChangeService.GetRequestByIdAsync(requestId);
            if (request == null || request.RequestedRoom == null)
            {
                return false;
            }

            var scopedHostelId = await _wardenContext.GetScopedHostelIdAsync(GetCurrentUserId());
            return scopedHostelId.HasValue && scopedHostelId.Value == request.RequestedRoom.HostelId;
        }

        private int GetCurrentUserId()
        {
            string? userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.Parse(userIdClaim!);
        }
    }
}