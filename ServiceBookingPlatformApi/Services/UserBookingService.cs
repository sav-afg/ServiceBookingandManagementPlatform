using Microsoft.EntityFrameworkCore;
using ServiceBookingPlatform.Data;
using ServiceBookingPlatform.Models;
using ServiceBookingPlatform.Models.Dtos.Booking;
using ServiceBookingPlatform.Services.Common;
using System.Security.Claims;

namespace ServiceBookingPlatform.Services
{
    public class UserBookingService(AppDbContext Db) : IUserBookingService
    {
        private async Task<BookingDto?> GetBookingDtoByIdAsync(Guid publicId)
        {
            return await Db.Bookings
                .Include(b => b.User)
                .Include(b => b.Service)
                .Where(b => b.PublicId == publicId && b.User != null && b.Service != null)
                .Select(b => new BookingDto(
                    b.Id,
                    b.PublicId,
                    b.ScheduledStart,
                    b.ScheduledEnd,
                    b.Status,
                    b.User!.LastName,
                    b.User.Email,
                    b.Service!.ServiceName
                ))
                .FirstOrDefaultAsync();
        }

        public async Task<List<BookingDto>> GetAllBookingsAsync(ClaimsPrincipal user)
        {
            var role = user.FindFirst(ClaimTypes.Role)?.Value;
            var userId = int.Parse(user.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var query = Db.Bookings
                .Include(b => b.User)
                .Include(b => b.Service)
                .Where(b => b.User != null && b.Service != null);

            if (role != "Admin" && role != "Staff")
                query = query.Where(b => b.UserId == userId);

            return await query
                .Select(b => new BookingDto(
                    b.Id,
                    b.PublicId,
                    b.ScheduledStart,
                    b.ScheduledEnd,
                    b.Status,
                    b.User!.LastName,
                    b.User.Email,
                    b.Service!.ServiceName
                ))
                .ToListAsync();
        }

        public async Task<BookingDto?> GetBookingByIdAsync(Guid publicId, ClaimsPrincipal user)
        {
            var booking = await Db.Bookings.FirstOrDefaultAsync(b => b.PublicId == publicId)
                ?? throw new NullReferenceException("Booking not found.");

            var role = user.FindFirst(ClaimTypes.Role)?.Value;
            var userId = int.Parse(user.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            if (role == "Customer" && booking.UserId != userId)
                throw new UnauthorizedAccessException("You do not have permission to access this booking.");

            return await GetBookingDtoByIdAsync(publicId);
        }

        public async Task<Result<BookingDto?>> CreateBookingAsync(int userId, CreateBookingDto newBooking)
        {
            if (newBooking.ScheduledEnd <= newBooking.ScheduledStart)
                return Result<BookingDto?>.Failure("Scheduled end time must be after scheduled start time.");

            if (newBooking.ScheduledStart < DateTime.UtcNow)
                return Result<BookingDto?>.Failure("Scheduled start time must be in the future.");

            var duration = newBooking.ScheduledEnd - newBooking.ScheduledStart;
            if (duration.TotalHours > 8)
                return Result<BookingDto?>.Failure("Booking duration cannot exceed 8 hours.");

            if (newBooking.Status != "Pending" && newBooking.Status != "Confirmed" && newBooking.Status != "Cancelled" && newBooking.Status != "Completed")
                return Result<BookingDto?>.Failure("Invalid booking status. Allowed values are: Pending, Confirmed, Cancelled, Completed.");

            var userExists = await Db.Users.AnyAsync(u => u.Id == userId);
            if (!userExists)
                return Result<BookingDto?>.Failure("The specified user does not exist.");

            var serviceExists = await Db.Services.AnyAsync(s => s.Id == newBooking.ServiceId);
            if (!serviceExists)
                return Result<BookingDto?>.Failure("The specified service does not exist.");

            bool bookingConflict = await Db.Bookings
                .AnyAsync(b => b.ServiceId == newBooking.ServiceId &&
                               ((newBooking.ScheduledStart >= b.ScheduledStart && newBooking.ScheduledStart < b.ScheduledEnd) ||
                                (newBooking.ScheduledEnd > b.ScheduledStart && newBooking.ScheduledEnd <= b.ScheduledEnd) ||
                                (newBooking.ScheduledStart <= b.ScheduledStart && newBooking.ScheduledEnd >= b.ScheduledEnd)));

            if (bookingConflict)
                return Result<BookingDto?>.Failure("The service is already booked for the requested time.");

            var booking = new Booking
            {
                UserId = userId,
                ServiceId = newBooking.ServiceId,
                ScheduledStart = newBooking.ScheduledStart,
                ScheduledEnd = newBooking.ScheduledEnd,
                Status = newBooking.Status
            };

            Db.Bookings.Add(booking);
            await Db.SaveChangesAsync();

            return Result<BookingDto?>.Success(
                await GetBookingDtoByIdAsync(booking.PublicId),
                "Booking created successfully.");
        }

        public async Task<Result<BookingDto?>> UpdateBookingAsync(Guid publicId, UpdateBookingDto updatedBooking, ClaimsPrincipal user)
        {
            var existingBooking = await Db.Bookings.FirstOrDefaultAsync(b => b.PublicId == publicId);

            if (existingBooking == null)
                return Result<BookingDto?>.Failure("Booking not found.");

            var role = user.FindFirst(ClaimTypes.Role)?.Value;
            var userId = int.Parse(user.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            if (role == "Customer" && existingBooking.UserId != userId)
                return Result<BookingDto?>.Failure("You do not have permission to update this booking.");

            if (role == "Customer" && (existingBooking.ScheduledStart - DateTime.UtcNow).TotalHours < 24)
                return Result<BookingDto?>.Failure("You cannot update a booking within 24 hours of the scheduled start time.");

            if (updatedBooking.ScheduledEnd <= updatedBooking.ScheduledStart)
                return Result<BookingDto?>.Failure("Scheduled end time must be after scheduled start time.");

            if (updatedBooking.ScheduledStart < DateTime.UtcNow)
                return Result<BookingDto?>.Failure("Scheduled start time must be in the future.");

            var duration = updatedBooking.ScheduledEnd - updatedBooking.ScheduledStart;
            if (duration.TotalHours > 8)
                return Result<BookingDto?>.Failure("Booking duration cannot exceed 8 hours.");

            if (updatedBooking.Status != "Pending" && updatedBooking.Status != "Confirmed" && updatedBooking.Status != "Cancelled" && updatedBooking.Status != "Completed")
                return Result<BookingDto?>.Failure("Invalid booking status. Allowed values are: Pending, Confirmed, Cancelled, Completed.");

            existingBooking.ScheduledStart = updatedBooking.ScheduledStart;
            existingBooking.ScheduledEnd = updatedBooking.ScheduledEnd;
            existingBooking.Status = updatedBooking.Status;

            await Db.SaveChangesAsync();

            return Result<BookingDto?>.Success(
                await GetBookingDtoByIdAsync(existingBooking.PublicId),
                "Booking updated successfully.");
        }

        public async Task<bool> DeleteBookingAsync(Guid publicId, ClaimsPrincipal user)
        {
            var booking = await Db.Bookings.FirstOrDefaultAsync(b => b.PublicId == publicId);

            if (booking == null)
                return false;

            var role = user.FindFirst(ClaimTypes.Role)?.Value;
            var userId = int.Parse(user.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            if (role != "Admin" && booking.UserId != userId)
                throw new UnauthorizedAccessException("You do not have permission to delete this booking.");

            if (role != "Admin" && (booking.ScheduledStart - DateTime.UtcNow).TotalHours < 24)
                throw new InvalidOperationException("You cannot delete a booking within 24 hours of the scheduled start time.");

            Db.Bookings.Remove(booking);
            await Db.SaveChangesAsync();
            return true;
        }
    }
}
