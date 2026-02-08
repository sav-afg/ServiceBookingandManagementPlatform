using ServiceBookingPlatform.Models.Dtos.Booking;
using System.Net.Http.Json;

namespace WasmClient.Api
{
    public interface IUserBookingApiClient
    {
        Task<List<BookingDto>> GetAllBookingsAsync();
        Task<BookingDto?> GetBookingByIdAsync(int id);
        Task<BookingDto> CreateBookingAsync(CreateBookingDto booking);
        Task<BookingDto> UpdateBookingAsync(int id, UpdateBookingDto booking);
        Task DeleteBookingAsync(int id);
    }

    public class UserBookingApiClient(HttpClient httpClient, ILogger<UserBookingApiClient> logger)
        : ApiClientBase(httpClient, logger), IUserBookingApiClient
    {
        public async Task<List<BookingDto>> GetAllBookingsAsync()
            => await GetAsync<List<BookingDto>>("api/userbooking") ?? new List<BookingDto>();

        public async Task<BookingDto?> GetBookingByIdAsync(int id)
            => await GetAsync<BookingDto>($"api/userbooking/{id}");

        public async Task<BookingDto> CreateBookingAsync(CreateBookingDto booking)
            => await PostAsync<CreateBookingDto, BookingDto>("api/userbooking", booking)
                ?? throw new InvalidOperationException("Booking creation failed");

        public async Task<BookingDto> UpdateBookingAsync(int id, UpdateBookingDto booking)
        {
            try
            {
                var response = await httpClient.PutAsJsonAsync($"api/userbooking/{id}", booking);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<BookingDto>()
                    ?? throw new InvalidOperationException("Booking update failed");
            }
            catch (HttpRequestException ex)
            {
                logger.LogError(ex, "PUT request failed: api/userbooking/{Id}", id);
                throw;
            }
        }

        public async Task DeleteBookingAsync(int id)
        {
            try
            {
                var response = await httpClient.DeleteAsync($"api/userbooking/{id}");
                response.EnsureSuccessStatusCode();
            }
            catch (HttpRequestException ex)
            {
                logger.LogError(ex, "DELETE request failed: api/userbooking/{Id}", id);
                throw;
            }
        }
    }
}
