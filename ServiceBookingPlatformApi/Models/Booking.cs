using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace ServiceBookingPlatform.Models
{
    [Index(nameof(PublicId), IsUnique = true)]
    public class Booking
    {
        [Key]
        public int Id { get; set; }

        // Public-facing URL identifier — non-enumerable, never the DB primary key
        public Guid PublicId { get; set; } = Guid.NewGuid();

        public int UserId { get; set; }
        public int ServiceId { get; set; }

        public DateTime ScheduledStart { get; set; }
        public DateTime ScheduledEnd { get; set; }
        public required string Status { get; set; }

        // Navigation properties
        public User? User { get; set; }
        public Service? Service { get; set; }
    }
}
