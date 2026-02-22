namespace ServiceBookingPlatform.Models.Dtos.Service
{
    public record ServiceDto(
        int Id,
        string ServiceName,
        string ServiceType,
        string ServiceDescription
    );
}
