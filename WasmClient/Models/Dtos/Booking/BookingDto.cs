namespace ServiceBookingPlatform.Models.Dtos.Booking
{
    public record BookingDto(
        int Id,
        DateTime ScheduledStart,
        DateTime ScheduledEnd,
        string Status,
        string LastName,
        string Email,
        string ServiceName
    );
}
