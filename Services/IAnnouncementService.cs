using Hostel_hub.Models;

namespace Hostel_hub.Services
{
    public interface IAnnouncementService
    {
        Task<List<Announcement>> GetActiveAnnouncementsAsync();
        Task<List<Announcement>> GetAllAnnouncementsAsync();
        Task<Announcement?> GetByIdAsync(int announcementId);
        Task<(bool Success, string? ErrorMessage)> CreateAnnouncementAsync(Announcement announcement);
        Task<(bool Success, string? ErrorMessage)> UpdateAnnouncementAsync(int announcementId, Announcement updated);
        Task<(bool Success, string? ErrorMessage)> DeactivateAnnouncementAsync(int announcementId);
    }
}