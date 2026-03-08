using ServiceBookingPlatform.Models;
using ServiceBookingPlatform.Models.Dtos.Booking;
using ServiceBookingPlatform.Services.Common;
using System.Security.Claims;
namespace ServiceBookingPlatform.Services
{
    public interface IUserBookingService
    {
        Task<List<BookingDto>> GetAllBookingsAsync(ClaimsPrincipal user);

        Task<BookingDto?> GetBookingByIdAsync(Guid publicId, ClaimsPrincipal user);

        Task<Result<BookingDto?>> CreateBookingAsync(int userId, CreateBookingDto newBooking);

        Task<Result<BookingDto?>> UpdateBookingAsync(Guid publicId, UpdateBookingDto updatedBooking, ClaimsPrincipal user);

        Task<bool> DeleteBookingAsync(Guid publicId, ClaimsPrincipal user);
    }
}
