using ServiceBookingPlatform.Models.Dtos.Booking;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace WasmClient.Api
{
    public interface IUserBookingApiClient
    {
        Task<List<BookingDto>> GetAllBookingsAsync();
        Task<BookingDto?> GetBookingByIdAsync(Guid publicId);
        Task<BookingDto> CreateBookingAsync(CreateBookingDto booking);
        Task<BookingDto> UpdateBookingAsync(Guid publicId, UpdateBookingDto booking);
        Task DeleteBookingAsync(Guid publicId);
    }

    public class UserBookingApiClient(HttpClient httpClient, ILogger<UserBookingApiClient> logger)
        : ApiClientBase(httpClient, logger), IUserBookingApiClient
    {
        public async Task<List<BookingDto>> GetAllBookingsAsync()
            => await GetAsync<List<BookingDto>>("api/userbooking") ?? [];

        public async Task<BookingDto?> GetBookingByIdAsync(Guid publicId)
            => await GetAsync<BookingDto>($"api/userbooking/{publicId}");

        public async Task<BookingDto> CreateBookingAsync(CreateBookingDto booking)
            => await PostAsync<CreateBookingDto, BookingDto>("api/userbooking", booking)
                ?? throw new InvalidOperationException("Booking creation failed");

        public async Task<BookingDto> UpdateBookingAsync(Guid publicId, UpdateBookingDto booking)
        {
            var response = await httpClient.PatchAsJsonAsync($"api/userbooking/{publicId}", booking);

            // Read the server's validation message from the response body before throwing,
            // so the UI can display it instead of a generic error
            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                var body = await response.Content.ReadFromJsonAsync<JsonElement>();
                var message = body.TryGetProperty("message", out var prop)
                    ? prop.GetString() ?? "Validation failed."
                    : "Validation failed.";
                throw new HttpRequestException(message, null, HttpStatusCode.BadRequest);
            }

            if (!response.IsSuccessStatusCode)
            {
                logger.LogError("PATCH request failed with {StatusCode}", (int)response.StatusCode);
                response.EnsureSuccessStatusCode();
            }

            return await response.Content.ReadFromJsonAsync<BookingDto>()
                ?? throw new InvalidOperationException("Booking update failed");
        }

        public async Task DeleteBookingAsync(Guid publicId)
            => await DeleteAsync($"api/userbooking/{publicId}");
    }
}
