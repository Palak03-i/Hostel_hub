using Microsoft.EntityFrameworkCore;
using Hostel_hub.Data;
using Hostel_hub.Models;

namespace Hostel_hub.Services
{
    public class AnnouncementService : IAnnouncementService
    {
        private readonly ApplicationDbContext _context;

        public AnnouncementService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Announcement>> GetActiveAnnouncementsAsync(int? hostelId)
        {
            var today = DateOnly.FromDateTime(DateTime.Today);

            return await _context.Announcements
                .Where(a => a.IsActive && a.PublishDate <= today && a.ExpiryDate >= today)
                .Where(a => a.HostelId == null || (hostelId.HasValue && a.HostelId == hostelId.Value))
                .OrderByDescending(a => a.IsImportant)
                .ThenByDescending(a => a.PublishDate)
                .ToListAsync();
        }

        public async Task<List<Announcement>> GetAllAnnouncementsAsync(int? scopedHostelId)
        {
            IQueryable<Announcement> query = _context.Announcements
                .Include(a => a.PostedByUser)
                .Include(a => a.Hostel);

            if (scopedHostelId.HasValue)
            {
                // Warden: see their own hostel's announcements plus global ones.
                query = query.Where(a => a.HostelId == scopedHostelId.Value || a.HostelId == null);
            }
            // Super Admin (scopedHostelId == null): sees everything, no filter.

            return await query.OrderByDescending(a => a.PostedAt).ToListAsync();
        }
        public async Task<Announcement?> GetByIdAsync(int announcementId)
        {
            return await _context.Announcements
                .Include(a => a.PostedByUser)
                .Include(a => a.Hostel)
                .FirstOrDefaultAsync(a => a.AnnouncementId == announcementId);
        }

        public async Task<(bool Success, string? ErrorMessage)> CreateAnnouncementAsync(Announcement announcement)
        {
            if (announcement.ExpiryDate < announcement.PublishDate)
            {
                return (false, "Expiry date cannot be before the publish date.");
            }

            _context.Announcements.Add(announcement);
            await _context.SaveChangesAsync();
            return (true, null);
        }

        public async Task<(bool Success, string? ErrorMessage)> UpdateAnnouncementAsync(int announcementId, Announcement updated)
        {
            if (updated.ExpiryDate < updated.PublishDate)
            {
                return (false, "Expiry date cannot be before the publish date.");
            }

            var announcement = await _context.Announcements.FirstOrDefaultAsync(a => a.AnnouncementId == announcementId);
            if (announcement == null)
            {
                return (false, "Announcement not found.");
            }

            announcement.Title = updated.Title;
            announcement.Content = updated.Content;
            announcement.PublishDate = updated.PublishDate;
            announcement.ExpiryDate = updated.ExpiryDate;
            announcement.IsImportant = updated.IsImportant;

            await _context.SaveChangesAsync();
            return (true, null);
        }

        public async Task<(bool Success, string? ErrorMessage)> DeactivateAnnouncementAsync(int announcementId)
        {
            var announcement = await _context.Announcements.FirstOrDefaultAsync(a => a.AnnouncementId == announcementId);
            if (announcement == null)
            {
                return (false, "Announcement not found.");
            }

            announcement.IsActive = false;
            await _context.SaveChangesAsync();
            return (true, null);
        }
    }
}