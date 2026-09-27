using Hostel_hub.Models;
using static Hostel_hub.Models.Complaint;

namespace Hostel_hub.Services
{
    public interface IComplaintService
    {
        Task<(bool Success, string? ErrorMessage)> CreateComplaintAsync(int studentId, string title, string description, ComplaintCategory category);
        Task<List<Complaint>> GetComplaintsForStudentAsync(int studentId);
        Task<List<Complaint>> GetAllComplaintsAsync(int? hostelId, ComplaintStatus? status, ComplaintCategory? category, ComplaintPriority? priority);
        Task<List<Complaint>> GetAssignedComplaintsAsync(int staffId);
        Task<Complaint?> GetComplaintByIdAsync(int complaintId);
        Task<List<ComplaintStatusHistory>> GetHistoryAsync(int complaintId);
        Task<(bool Success, string? ErrorMessage)> SetPriorityAsync(int complaintId, ComplaintPriority priority, string changedBy);
        Task<(bool Success, string? ErrorMessage)> AssignStaffAsync(int complaintId, int staffId, string changedBy);
        Task<(bool Success, string? ErrorMessage)> UpdateStatusAsync(int complaintId, ComplaintStatus newStatus, string changedBy, string? remarks);
    }
}