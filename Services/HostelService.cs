using Microsoft.EntityFrameworkCore;
using Hostel_hub.Data;
using Hostel_hub.Models;

namespace Hostel_hub.Services
{
    public class HostelService : IHostelService
    {
        private readonly ApplicationDbContext _context;

        public HostelService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Hostel>> GetAllHostelsAsync()
        {
            return await _context.Hostels
                .Include(h => h.Rooms)
                .OrderBy(h => h.Name)
                .ToListAsync();
        }

        public async Task<Hostel?> GetHostelByIdAsync(int hostelId)
        {
            return await _context.Hostels
                .Include(h => h.Rooms)
                .FirstOrDefaultAsync(h => h.HostelId == hostelId);
        }

        public async Task CreateHostelAsync(Hostel hostel)
        {
            _context.Hostels.Add(hostel);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> UpdateHostelAsync(int hostelId, string name, HostelType type)
        {
            var hostel = await _context.Hostels.FirstOrDefaultAsync(h => h.HostelId == hostelId);

            if (hostel == null)
            {
                return false;
            }

            hostel.Name = name;
            hostel.Type = type;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<(bool Success, string? ErrorMessage)> DeleteHostelAsync(int hostelId)
        {
            var hostel = await _context.Hostels
                .Include(h => h.Rooms)
                .FirstOrDefaultAsync(h => h.HostelId == hostelId);

            if (hostel == null)
            {
                return (false, "Hostel not found.");
            }

            if (hostel.Rooms.Any())
            {
                return (false, $"Cannot delete '{hostel.Name}' — it still has {hostel.Rooms.Count} room(s). Remove or reassign its rooms first.");
            }

            _context.Hostels.Remove(hostel);
            await _context.SaveChangesAsync();
            return (true, null);
        }
    }
}