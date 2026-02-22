using System.ComponentModel.DataAnnotations;

namespace ServiceBookingPlatform.Models.Dtos.Booking
{
    public record CreateBookingDto
    {
        [Required(ErrorMessage = "Booking must be associated with a service.")]
        public required int ServiceId { get; init; }

        [Required(ErrorMessage = "Booking must have a scheduled start time.")]
        public required DateTime ScheduledStart { get; init; }

        [Required(ErrorMessage = "Booking must have a scheduled end time.")]
        public required DateTime ScheduledEnd { get; init; }

        [Required(ErrorMessage = "Booking status is required.")]
        public required string Status { get; init; }
    }
}
