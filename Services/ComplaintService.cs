using Hostel_hub.Data;
using Hostel_hub.Models;
using Microsoft.EntityFrameworkCore;
using static Hostel_hub.Models.Complaint;

namespace Hostel_hub.Services
{
    public class ComplaintService : IComplaintService
    {
        private readonly ApplicationDbContext _context;

        // Defines exactly which status transitions are legal.
        private static readonly Dictionary<ComplaintStatus, List<ComplaintStatus>> AllowedTransitions = new()
        {
            { ComplaintStatus.Pending, new List<ComplaintStatus> { ComplaintStatus.Assigned } },
            { ComplaintStatus.Assigned, new List<ComplaintStatus> { ComplaintStatus.InProgress } },
            { ComplaintStatus.InProgress, new List<ComplaintStatus> { ComplaintStatus.Resolved } },
            { ComplaintStatus.Resolved, new List<ComplaintStatus>() }
        };

        public ComplaintService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<(bool Success, string? ErrorMessage)> CreateComplaintAsync(int studentId, string title, string description, ComplaintCategory category)
        {
            var student = await _context.Students.FirstOrDefaultAsync(s => s.StudentId == studentId);
            if (student == null) return (false, "Student not found.");
            if (!student.RoomId.HasValue) return (false, "You must have a room assigned before filing a complaint.");

            var complaint = new Complaint
            {
                StudentId = studentId,
                Title = title,
                Description = description,
                Category = category,
                Priority = ComplaintPriority.Low,
                Status = ComplaintStatus.Pending
            };

            _context.Complaints.Add(complaint);
            await _context.SaveChangesAsync();

            await AddHistoryAsync(complaint.ComplaintId, ComplaintStatus.Pending, ComplaintStatus.Pending, "Student", "Complaint created.");

            return (true, null);
        }

        public async Task<List<Complaint>> GetComplaintsForStudentAsync(int studentId)
        {
            return await _context.Complaints
                .Include(c => c.AssignedStaff)
                .Where(c => c.StudentId == studentId)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<Complaint>> GetAllComplaintsAsync(int? hostelId, ComplaintStatus? status, ComplaintCategory? category, ComplaintPriority? priority)
        {
            IQueryable<Complaint> query = _context.Complaints
                .Include(c => c.Student)
                .Include(c => c.AssignedStaff);

            if (hostelId.HasValue)
            {
                query = query.Where(c => c.Student!.HostelId == hostelId.Value);
            }
            if (status.HasValue)
            {
                query = query.Where(c => c.Status == status.Value);
            }
            if (category.HasValue)
            {
                query = query.Where(c => c.Category == category.Value);
            }
            if (priority.HasValue)
            {
                query = query.Where(c => c.Priority == priority.Value);
            }

            return await query.OrderByDescending(c => c.CreatedAt).ToListAsync();
        }

        public async Task<List<Complaint>> GetAssignedComplaintsAsync(int staffId)
        {
            return await _context.Complaints
                .Include(c => c.Student)
                .Where(c => c.AssignedStaffId == staffId && c.Status != ComplaintStatus.Resolved)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
        }

        public async Task<Complaint?> GetComplaintByIdAsync(int complaintId)
        {
            return await _context.Complaints
                .Include(c => c.Student)
                .Include(c => c.AssignedStaff)
                .FirstOrDefaultAsync(c => c.ComplaintId == complaintId);
        }

        public async Task<List<ComplaintStatusHistory>> GetHistoryAsync(int complaintId)
        {
            return await _context.ComplaintStatusHistories
                .Where(h => h.ComplaintId == complaintId)
                .OrderBy(h => h.ChangedAt)
                .ToListAsync();
        }

        public async Task<(bool Success, string? ErrorMessage)> SetPriorityAsync(int complaintId, ComplaintPriority priority, string changedBy)
        {
            var complaint = await _context.Complaints.FirstOrDefaultAsync(c => c.ComplaintId == complaintId);
            if (complaint == null) return (false, "Complaint not found.");

            complaint.Priority = priority;
            await _context.SaveChangesAsync();
            return (true, null);
        }

        public async Task<(bool Success, string? ErrorMessage)> AssignStaffAsync(int complaintId, int staffId, string changedBy)
        {
            var complaint = await _context.Complaints.FirstOrDefaultAsync(c => c.ComplaintId == complaintId);
            if (complaint == null) return (false, "Complaint not found.");

            if (complaint.Status == ComplaintStatus.Resolved)
            {
                return (false, "Cannot assign a resolved complaint.");
            }

            var staff = await _context.MaintenanceStaff.FirstOrDefaultAsync(s => s.MaintenanceStaffId == staffId);
            if (staff == null) return (false, "Staff member not found.");

            var oldStatus = complaint.Status;
            complaint.AssignedStaffId = staffId;
            complaint.Status = ComplaintStatus.Assigned;
            await _context.SaveChangesAsync();

            await AddHistoryAsync(complaintId, oldStatus, ComplaintStatus.Assigned, changedBy, $"Assigned to {staff.FullName}.");

            return (true, null);
        }

        public async Task<(bool Success, string? ErrorMessage)> UpdateStatusAsync(int complaintId, ComplaintStatus newStatus, string changedBy, string? remarks)
        {
            var complaint = await _context.Complaints.FirstOrDefaultAsync(c => c.ComplaintId == complaintId);
            if (complaint == null) return (false, "Complaint not found.");

            var oldStatus = complaint.Status;

            if (!AllowedTransitions[oldStatus].Contains(newStatus))
            {
                return (false, $"Cannot change status from {oldStatus} to {newStatus}. Only {string.Join(", ", AllowedTransitions[oldStatus])} allowed.");
            }

            complaint.Status = newStatus;
            if (newStatus == ComplaintStatus.Resolved)
            {
                complaint.ResolvedDate = DateTime.UtcNow;
            }
            await _context.SaveChangesAsync();

            await AddHistoryAsync(complaintId, oldStatus, newStatus, changedBy, remarks);

            return (true, null);
        }

        private async Task AddHistoryAsync(int complaintId, ComplaintStatus oldStatus, ComplaintStatus newStatus, string changedBy, string? remarks)
        {
            _context.ComplaintStatusHistories.Add(new ComplaintStatusHistory
            {
                ComplaintId = complaintId,
                OldStatus = oldStatus,
                NewStatus = newStatus,
                ChangedBy = changedBy,
                Remarks = remarks
            });
            await _context.SaveChangesAsync();
        }
    }
}