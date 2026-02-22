namespace ServiceBookingPlatform.Models.Dtos.Booking
{
    public record UpdateBookingDto
    {
        public required DateTime ScheduledStart { get; init; }
        public required DateTime ScheduledEnd { get; init; }
        public required string Status { get; init; }
    }
}
