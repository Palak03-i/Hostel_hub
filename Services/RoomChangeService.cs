using Microsoft.EntityFrameworkCore;
using Hostel_hub.Data;
using Hostel_hub.Models;

namespace Hostel_hub.Services
{
    public class RoomChangeService : IRoomChangeService
    {
        private readonly ApplicationDbContext _context;

        public RoomChangeService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<(bool Success, string? ErrorMessage)> CreateRequestAsync(int studentId, int requestedRoomId, string reason)
        {
            var student = await _context.Students.FirstOrDefaultAsync(s => s.StudentId == studentId);
            if (student == null)
            {
                return (false, "Student not found.");
            }

            if (!student.RoomId.HasValue)
            {
                return (false, "You must have an existing room before requesting a change.");
            }

            if (student.RoomId.Value == requestedRoomId)
            {
                return (false, "You are already in this room.");
            }

            var requestedRoom = await _context.Rooms.FirstOrDefaultAsync(r => r.RoomId == requestedRoomId);
            if (requestedRoom == null)
            {
                return (false, "Requested room not found.");
            }

            bool hasPendingRequest = await _context.RoomChangeRequests
                .AnyAsync(r => r.StudentId == studentId && r.Status == RoomChangeStatus.Pending);
            if (hasPendingRequest)
            {
                return (false, "You already have a pending room change request.");
            }

            var request = new RoomChangeRequest
            {
                StudentId = studentId,
                CurrentRoomId = student.RoomId.Value,
                RequestedRoomId = requestedRoomId,
                Reason = reason,
                Status = RoomChangeStatus.Pending
            };

            _context.RoomChangeRequests.Add(request);
            await _context.SaveChangesAsync();
            return (true, null);
        }

        public async Task<List<RoomChangeRequest>> GetRequestsForStudentAsync(int studentId)
        {
            return await _context.RoomChangeRequests
                .Include(r => r.CurrentRoom)
                .Include(r => r.RequestedRoom)
                .Where(r => r.StudentId == studentId)
                .OrderByDescending(r => r.RequestedAt)
                .ToListAsync();
        }

        public async Task<List<RoomChangeRequest>> GetPendingRequestsAsync(int? hostelId)
        {
            IQueryable<RoomChangeRequest> query = _context.RoomChangeRequests
                .Include(r => r.Student)
                .Include(r => r.CurrentRoom)
                .Include(r => r.RequestedRoom)
                .Where(r => r.Status == RoomChangeStatus.Pending);

            if (hostelId.HasValue)
            {
                query = query.Where(r => r.RequestedRoom!.HostelId == hostelId.Value);
            }

            return await query.OrderBy(r => r.RequestedAt).ToListAsync();
        }

        public async Task<RoomChangeRequest?> GetRequestByIdAsync(int requestId)
        {
            return await _context.RoomChangeRequests
                .Include(r => r.Student)
                .Include(r => r.CurrentRoom)
                .Include(r => r.RequestedRoom)
                .FirstOrDefaultAsync(r => r.RoomChangeRequestId == requestId);
        }

        public async Task<(bool Success, string? ErrorMessage)> ApproveRequestAsync(int requestId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var request = await _context.RoomChangeRequests
                    .FirstOrDefaultAsync(r => r.RoomChangeRequestId == requestId);

                if (request == null)
                {
                    return (false, "Request not found.");
                }

                if (request.Status != RoomChangeStatus.Pending)
                {
                    return (false, $"This request has already been {request.Status}. It cannot be approved again.");
                }

                var student = await _context.Students.FirstOrDefaultAsync(s => s.StudentId == request.StudentId);
                var oldRoom = await _context.Rooms.FirstOrDefaultAsync(r => r.RoomId == request.CurrentRoomId);
                var newRoom = await _context.Rooms.FirstOrDefaultAsync(r => r.RoomId == request.RequestedRoomId);

                if (student == null || oldRoom == null || newRoom == null)
                {
                    return (false, "Related student or room data could not be found.");
                }

                // CRITICAL: re-check capacity NOW, at approval time — not using
                // any value from when the request was originally created.
                if (newRoom.CurrentOccupancy >= newRoom.Capacity)
                {
                    return (false, $"Cannot approve — room '{newRoom.RoomNumber}' is now full ({newRoom.CurrentOccupancy}/{newRoom.Capacity}).");
                }

                oldRoom.CurrentOccupancy -= 1;
                newRoom.CurrentOccupancy += 1;
                student.RoomId = newRoom.RoomId;
                student.HostelId = newRoom.HostelId;
                request.Status = RoomChangeStatus.Approved;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return (true, null);
            }
            catch (DbUpdateConcurrencyException)
            {
                await transaction.RollbackAsync();
                return (false, "One of the rooms was just modified by someone else. Please refresh and try again.");
            }
        }

        public async Task<(bool Success, string? ErrorMessage)> RejectRequestAsync(int requestId)
        {
            var request = await _context.RoomChangeRequests.FirstOrDefaultAsync(r => r.RoomChangeRequestId == requestId);
            if (request == null)
            {
                return (false, "Request not found.");
            }

            if (request.Status != RoomChangeStatus.Pending)
            {
                return (false, $"This request has already been {request.Status}.");
            }

            request.Status = RoomChangeStatus.Rejected;
            await _context.SaveChangesAsync();
            return (true, null);
        }
    }
}