using Microsoft.EntityFrameworkCore;
using Hostel_hub.Models;

namespace Hostel_hub.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Student> Students { get; set; }
        public DbSet<Hostel> Hostels { get; set; }
        public DbSet<Room> Rooms { get; set; }
        public DbSet<MaintenanceStaff> MaintenanceStaff { get; set; }
        public DbSet<Complaint> Complaints { get; set; }
        public DbSet<RoomChangeRequest> RoomChangeRequests { get; set; }
        public DbSet<Announcement> Announcements { get; set; }
        public DbSet<Feedback> Feedbacks { get; set; }
        public DbSet<Warden> Wardens { get; set; }
        public DbSet<ComplaintStatusHistory> ComplaintStatusHistories { get; set; }
        public DbSet<MessMenu> MessMenus { get; set; }
        public DbSet<MessMenuItem> MessMenuItems { get; set; }
        public DbSet<MealSelection> MealSelections { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Unique constraints
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<Student>()
                .HasIndex(s => s.RollNumber)
                .IsUnique();

            modelBuilder.Entity<Room>()
                .HasIndex(r => new { r.HostelId, r.RoomNumber })
                .IsUnique();

            modelBuilder.Entity<Feedback>()
                .HasIndex(f => f.ComplaintId)
                .IsUnique();

            // Delete behavior: prevent accidental cascading deletes
            // where they could silently destroy important records.

            modelBuilder.Entity<Student>()
                .HasOne(s => s.User)
                .WithOne()
                .HasForeignKey<Student>(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Complaint>()
                .HasOne(c => c.Student)
                .WithMany()
                .HasForeignKey(c => c.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<RoomChangeRequest>()
                .HasOne(r => r.CurrentRoom)
                .WithMany()
                .HasForeignKey(r => r.CurrentRoomId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<RoomChangeRequest>()
                .HasOne(r => r.RequestedRoom)
                .WithMany()
                .HasForeignKey(r => r.RequestedRoomId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Room>()
                .HasOne(r => r.Hostel)
                .WithMany(h => h.Rooms)
                .HasForeignKey(r => r.HostelId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Warden>()
                .HasIndex(w => w.UserId)
                .IsUnique();

            modelBuilder.Entity<Warden>()
                .HasIndex(w => w.HostelId)
                .IsUnique()
                .HasFilter("[HostelId] IS NOT NULL");
            modelBuilder.Entity<Warden>()
                .HasOne(w => w.User)
                .WithOne()
                .HasForeignKey<Warden>(w => w.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Warden>()
                .HasOne(w => w.Hostel)
                .WithMany()
                .HasForeignKey(w => w.HostelId)
                .OnDelete(DeleteBehavior.SetNull);
            modelBuilder.Entity<MessMenu>()
    .HasIndex(m => new { m.HostelId, m.MenuDate })
    .IsUnique();

            modelBuilder.Entity<MessMenu>()
                .HasOne(m => m.Hostel)
                .WithMany()
                .HasForeignKey(m => m.HostelId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<MessMenuItem>()
                .HasOne(i => i.MessMenu)
                .WithMany(m => m.Items)
                .HasForeignKey(i => i.MessMenuId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<MealSelection>()
                .HasIndex(s => new { s.StudentId, s.MessMenuId, s.MealType })
                .IsUnique();

            modelBuilder.Entity<MealSelection>()
                .HasOne(s => s.Student)
                .WithMany()
                .HasForeignKey(s => s.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<MealSelection>()
                .HasOne(s => s.MessMenu)
                .WithMany()
                .HasForeignKey(s => s.MessMenuId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}