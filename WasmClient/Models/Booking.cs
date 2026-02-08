using System.ComponentModel.DataAnnotations;
namespace ServiceBookingPlatform.Models
{
    public class Booking
    {
        [Key]
        public int Id { get; set; }
        public int UserId { get; set; }
        public int ServiceId { get; set; }

        public DateTime ScheduledStart { get; set; }
        public DateTime ScheduledEnd { get; set; }
        public required string Status { get; set; }

        // Navigation properties
        public User? User { get; set; }  // Use this to access user details
        public Service? Service { get; set; }
    }

}
