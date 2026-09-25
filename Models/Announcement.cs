using System.ComponentModel.DataAnnotations;

namespace Hostel_hub.Models
{
    public class Announcement
    {
        public int AnnouncementId { get; set; }

        [Required]
        [MaxLength(150)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [MaxLength(2000)]
        public string Content { get; set; } = string.Empty;

        [Required]
        public int PostedByUserId { get; set; }
        public User? PostedByUser { get; set; }

        public DateTime PostedAt { get; set; } = DateTime.UtcNow;
    }
}