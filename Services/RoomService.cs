using Microsoft.EntityFrameworkCore;
using Hostel_hub.Data;
using Hostel_hub.Models;

namespace Hostel_hub.Services
{
    public class RoomService : IRoomService
    {
        private readonly ApplicationDbContext _context;

        public RoomService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Room>> GetRoomsByHostelAsync(int? hostelId)
        {
            IQueryable<Room> query = _context.Rooms.Include(r => r.Hostel);

            if (hostelId.HasValue)
            {
                query = query.Where(r => r.HostelId == hostelId.Value);
            }

            return await query.OrderBy(r => r.Hostel!.Name).ThenBy(r => r.RoomNumber).ToListAsync();
        }

        public async Task<Room?> GetRoomByIdAsync(int roomId)
        {
            return await _context.Rooms
                .Include(r => r.Hostel)
                .Include(r => r.Students)
                .FirstOrDefaultAsync(r => r.RoomId == roomId);
        }

        public async Task<(bool Success, string? ErrorMessage)> CreateRoomAsync(int hostelId, string roomNumber, int capacity)
        {
            if (capacity <= 0)
            {
                return (false, "Capacity must be greater than zero.");
            }

            bool hostelExists = await _context.Hostels.AnyAsync(h => h.HostelId == hostelId);
            if (!hostelExists)
            {
                return (false, "Selected hostel does not exist.");
            }

            bool duplicateRoomNumber = await _context.Rooms
                .AnyAsync(r => r.HostelId == hostelId && r.RoomNumber == roomNumber);
            if (duplicateRoomNumber)
            {
                return (false, $"Room number '{roomNumber}' already exists in this hostel.");
            }

            var room = new Room
            {
                HostelId = hostelId,
                RoomNumber = roomNumber,
                Capacity = capacity,
                CurrentOccupancy = 0
            };

            _context.Rooms.Add(room);
            await _context.SaveChangesAsync();
            return (true, null);
        }

        public async Task<(bool Success, string? ErrorMessage)> UpdateRoomAsync(int roomId, string roomNumber, int capacity)
        {
            var room = await _context.Rooms.FirstOrDefaultAsync(r => r.RoomId == roomId);
            if (room == null)
            {
                return (false, "Room not found.");
            }

            if (capacity <= 0)
            {
                return (false, "Capacity must be greater than zero.");
            }

            if (capacity < room.CurrentOccupancy)
            {
                return (false, $"Cannot set capacity below current occupancy ({room.CurrentOccupancy}).");
            }

            bool duplicateRoomNumber = await _context.Rooms
                .AnyAsync(r => r.HostelId == room.HostelId && r.RoomNumber == roomNumber && r.RoomId != roomId);
            if (duplicateRoomNumber)
            {
                return (false, $"Room number '{roomNumber}' already exists in this hostel.");
            }

            room.RoomNumber = roomNumber;
            room.Capacity = capacity;
            await _context.SaveChangesAsync();
            return (true, null);
        }

        public async Task<(bool Success, string? ErrorMessage)> DeleteRoomAsync(int roomId)
        {
            var room = await _context.Rooms
                .Include(r => r.Students)
                .FirstOrDefaultAsync(r => r.RoomId == roomId);

            if (room == null)
            {
                return (false, "Room not found.");
            }

            if (room.Students.Any())
            {
                return (false, $"Cannot delete room '{room.RoomNumber}' — it has {room.Students.Count} student(s) currently allocated.");
            }

            _context.Rooms.Remove(room);
            await _context.SaveChangesAsync();
            return (true, null);
        }
    }
}