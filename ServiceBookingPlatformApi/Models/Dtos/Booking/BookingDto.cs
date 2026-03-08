namespace ServiceBookingPlatform.Models.Dtos.Booking
{
    public record BookingDto(
        int Id,
        Guid PublicId,
        DateTime ScheduledStart,
        DateTime ScheduledEnd,
        string Status,
        string LastName,
        string Email,
        string ServiceName
    );
}
