using Microsoft.EntityFrameworkCore;
using Hostel_hub.Data;
using Hostel_hub.Models;

namespace Hostel_hub.Services
{
    public class FeedbackService : IFeedbackService
    {
        private readonly ApplicationDbContext _context;

        public FeedbackService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Feedback>> GetFeedbackForStudentAsync(int studentId)
        {
            return await _context.Feedbacks
                .Include(f => f.Complaint)
                .Where(f => f.StudentId == studentId)
                .OrderByDescending(f => f.SubmittedAt)
                .ToListAsync();
        }

        public async Task<List<Feedback>> GetAllFeedbackAsync(FeedbackSource? source)
        {
            IQueryable<Feedback> query = _context.Feedbacks
                .Include(f => f.Student)
                .Include(f => f.Complaint);

            if (source.HasValue)
            {
                query = query.Where(f => f.Source == source.Value);
            }

            return await query.OrderByDescending(f => f.SubmittedAt).ToListAsync();
        }

        public async Task<(bool Success, string? ErrorMessage)> SubmitFeedbackAsync(int studentId, FeedbackSource source, int? complaintId, int rating, string? comments)
        {
            if (source == FeedbackSource.Complaint)
            {
                if (complaintId == null)
                {
                    return (false, "A complaint must be selected for complaint feedback.");
                }

                var complaint = await _context.Complaints.FirstOrDefaultAsync(c => c.ComplaintId == complaintId.Value);
                if (complaint == null || complaint.StudentId != studentId)
                {
                    return (false, "Complaint not found.");
                }

                if (complaint.Status != ComplaintStatus.Resolved)
                {
                    return (false, "Feedback can only be submitted for resolved complaints.");
                }

                bool alreadySubmitted = await _context.Feedbacks
                    .AnyAsync(f => f.StudentId == studentId && f.ComplaintId == complaintId.Value);
                if (alreadySubmitted)
                {
                    return (false, "You have already submitted feedback for this complaint.");
                }
            }
            else
            {
                // Mess/General feedback carries no ComplaintId at all.
                complaintId = null;
            }

            _context.Feedbacks.Add(new Feedback
            {
                StudentId = studentId,
                Source = source,
                ComplaintId = complaintId,
                Rating = rating,
                Comments = comments
            });

            await _context.SaveChangesAsync();
            return (true, null);
        }
    }
}