using Microsoft.EntityFrameworkCore;
using Hostel_hub.Data;
using Hostel_hub.Models;

namespace Hostel_hub.Services
{
    public class MaintenanceStaffService : IMaintenanceStaffService
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuthService _authService;

        public MaintenanceStaffService(ApplicationDbContext context, IAuthService authService)
        {
            _context = context;
            _authService = authService;
        }

        public async Task<List<MaintenanceStaff>> GetAllAsync(string? search)
        {
            IQueryable<MaintenanceStaff> query = _context.MaintenanceStaff.Include(m => m.User);

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(m => m.FullName.Contains(search));
            }

            return await query.OrderBy(m => m.FullName).ToListAsync();
        }

        public async Task<MaintenanceStaff?> GetByIdAsync(int staffId)
        {
            return await _context.MaintenanceStaff.Include(m => m.User).FirstOrDefaultAsync(m => m.MaintenanceStaffId == staffId);
        }

        public async Task<(bool Success, string? ErrorMessage)> CreateAsync(string email, string password, string fullName, string? phoneNumber, StaffSpecialization specialization)
        {
            bool emailExists = await _context.Users.AnyAsync(u => u.Email == email);
            if (emailExists)
            {
                return (false, "This email is already registered.");
            }

            var user = new User
            {
                Email = email,
                PasswordHash = _authService.HashPassword(password),
                Role = UserRole.MaintenanceStaff
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var staff = new MaintenanceStaff
            {
                UserId = user.UserId,
                FullName = fullName,
                PhoneNumber = phoneNumber,
                Specialization = specialization
            };
            _context.MaintenanceStaff.Add(staff);
            await _context.SaveChangesAsync();

            return (true, null);
        }

        public async Task<bool> UpdateAsync(int staffId, string fullName, string? phoneNumber, StaffSpecialization specialization)
        {
            var staff = await _context.MaintenanceStaff.FirstOrDefaultAsync(m => m.MaintenanceStaffId == staffId);
            if (staff == null) return false;

            staff.FullName = fullName;
            staff.PhoneNumber = phoneNumber;
            staff.Specialization = specialization;
            await _context.SaveChangesAsync();
            return true;
        }
    }
}