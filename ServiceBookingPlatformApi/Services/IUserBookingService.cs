using ServiceBookingPlatform.Models;
using ServiceBookingPlatform.Models.Dtos.Booking;
using ServiceBookingPlatform.Services.Common;
using System.Security.Claims;
namespace ServiceBookingPlatform.Services
{
    public interface IUserBookingService
    {
        Task<List<BookingDto>> GetAllBookingsAsync(ClaimsPrincipal user);

        Task<BookingDto?> GetBookingByIdAsync(int bookingId, ClaimsPrincipal user);

        Task<Result<BookingDto?>> CreateBookingAsync(int userId, CreateBookingDto newBooking);

        Task<Result<BookingDto?>> UpdateBookingAsync(int bookingId, UpdateBookingDto updatedBooking, ClaimsPrincipal user);

        Task<bool> DeleteBookingAsync(int bookingId, ClaimsPrincipal user);
    }
}
