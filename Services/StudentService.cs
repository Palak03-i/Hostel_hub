using Microsoft.EntityFrameworkCore;
using Hostel_hub.Data;
using Hostel_hub.Models;

namespace Hostel_hub.Services
{
    public class StudentService : IStudentService
    {
        private readonly ApplicationDbContext _context;

        public StudentService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Student?> GetProfileByUserIdAsync(int userId)
        {
            return await _context.Students
                .Include(s => s.Hostel)
                .Include(s => s.Room)
                .FirstOrDefaultAsync(s => s.UserId == userId);
        }

        public async Task<bool> UpdateProfileAsync(int userId, string fullName, string? phoneNumber)
        {
            var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == userId);

            if (student == null)
            {
                return false;
            }

            student.FullName = fullName;
            student.PhoneNumber = phoneNumber;

            await _context.SaveChangesAsync();
            return true;
        }
        public async Task<List<Student>> GetAllStudentsAsync(string? searchTerm, int? hostelId)
        {
            IQueryable<Student> query = _context.Students
                .Include(s => s.Hostel)
                .Include(s => s.Room);

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(s =>
                    s.FullName.Contains(searchTerm) ||
                    s.RollNumber.Contains(searchTerm));
            }

            if (hostelId.HasValue)
            {
                query = query.Where(s => s.HostelId == hostelId.Value);
            }

            return await query.OrderBy(s => s.FullName).ToListAsync();
        }

        public async Task<Student?> GetStudentByIdAsync(int studentId)
        {
            return await _context.Students
                .Include(s => s.Hostel)
                .Include(s => s.Room)
                .FirstOrDefaultAsync(s => s.StudentId == studentId);
        }
        public async Task<List<Student>> GetUnallocatedStudentsAsync(int hostelId)
        {
            return await _context.Students
                .Where(s =>
                    s.RoomId == null &&
                    s.HostelId == hostelId)
                .OrderBy(s => s.FullName)
                .ToListAsync();
        }

    }
}