using ServiceBookingPlatform.Models.Dtos.Booking;

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
            => await GetAsync<List<BookingDto>>("api/userbooking") ?? [];

        public async Task<BookingDto?> GetBookingByIdAsync(int id)
            => await GetAsync<BookingDto>($"api/userbooking/{id}");

        public async Task<BookingDto> CreateBookingAsync(CreateBookingDto booking)
            => await PostAsync<CreateBookingDto, BookingDto>("api/userbooking", booking)
                ?? throw new InvalidOperationException("Booking creation failed");

        public async Task<BookingDto> UpdateBookingAsync(int id, UpdateBookingDto booking)
            => await PatchAsync<UpdateBookingDto, BookingDto>($"api/userbooking/{id}", booking)
                ?? throw new InvalidOperationException("Booking update failed");

        public async Task DeleteBookingAsync(int id)
            => await DeleteAsync($"api/userbooking/{id}");
    }
}
