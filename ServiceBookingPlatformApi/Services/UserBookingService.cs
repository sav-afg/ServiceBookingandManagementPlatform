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
        private IQueryable<BookingDto> GetBookingQuery()
        {
            return Db.Bookings
                .Include(b => b.User)
                .Include(b => b.Service)
                .Where(b => b.User != null && b.Service != null)
                .Select(b => new BookingDto(
                    b.Id,
                    b.ScheduledStart,
                    b.ScheduledEnd,
                    b.Status,
                    b.User!.LastName,
                    b.User.Email,
                    b.Service!.ServiceName
                ));
        }

        private async Task<BookingDto?> GetBookingDtoByIdAsync(int bookingId)
        {
            return await GetBookingQuery()
                .FirstOrDefaultAsync(b => b.Id == bookingId);
        }

        public async Task<List<BookingDto>> GetAllBookingsAsync(ClaimsPrincipal user)
        {
            // Get user role and ID
            var role = user.FindFirst(ClaimTypes.Role)?.Value;
            var userId = int.Parse(user.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            // Base query
            var query = Db.Bookings
                .Include(b => b.User)
                .Include(b => b.Service)
                .Where(b => b.User != null && b.Service != null);

            // If user is not Admin or Staff, filter to only their bookings
            if (role != "Admin" && role != "Staff")
            {
                query = query.Where(b => b.UserId == userId);
            }

            return await query
                .Select(b => new BookingDto(
                    b.Id,
                    b.ScheduledStart,
                    b.ScheduledEnd,
                    b.Status,
                    b.User!.LastName,
                    b.User.Email,
                    b.Service!.ServiceName
                ))
                .ToListAsync();
        }

        public async Task<BookingDto?> GetBookingByIdAsync(int bookingId, ClaimsPrincipal user)
        {
            /*The exceptions thrown here are handled by the controller.
             Customers can only see their own bookings
            Admin and Staff can see all bookings*/

            var booking = await Db.Bookings.FindAsync(bookingId) ?? throw new NullReferenceException("Booking not found.");

            var role = user.FindFirst(ClaimTypes.Role)?.Value;
            var userId = int.Parse(user.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            if (role == "Customer" && booking.UserId != userId)
                throw new UnauthorizedAccessException("You do not have permission to access this booking.");

            var bookingDto = await GetBookingQuery()
                .FirstOrDefaultAsync(b => b.Id == bookingId);

            return bookingDto;
        }

        public async Task<Result<BookingDto?>> CreateBookingAsync(int userId, CreateBookingDto newBooking)
        {
            // Validate scheduled times
            if (newBooking.ScheduledEnd <= newBooking.ScheduledStart)
            {
                return Result<BookingDto?>.Failure("Scheduled end time must be after scheduled start time.");
            }

            // Validate scheduled times are in the future
            if (newBooking.ScheduledStart < DateTime.UtcNow)
            {
                return Result<BookingDto?>.Failure("Scheduled start time must be in the future.");
            }

            // Validate booking duration (max 8 hours)
            var duration = newBooking.ScheduledEnd - newBooking.ScheduledStart;
            if (duration.TotalHours > 8)
            {
                return Result<BookingDto?>.Failure("Booking duration cannot exceed 8 hours.");
            }

            // Validate booking status (can only take 4 values)
            if (newBooking.Status != "Pending" && newBooking.Status != "Confirmed" && newBooking.Status != "Cancelled" && newBooking.Status != "Completed")
            {
                return Result<BookingDto?>.Failure("Invalid booking status. Allowed values are: Pending, Confirmed, Cancelled, Completed.");
            }

            // Validate user exists
            var userExists = await Db.Users.AnyAsync(u => u.Id == userId);
            if (!userExists)
            {
                return Result<BookingDto?>.Failure("The specified user does not exist.");
            }

            // Validate service exists
            var serviceExists = await Db.Services.AnyAsync(s => s.Id == newBooking.ServiceId);
            if (!serviceExists)
            {
                return Result<BookingDto?>.Failure("The specified service does not exist.");
            }



            //Check to see if the service has been booked for the requested time
            bool bookingConflict = await Db.Bookings
                .AnyAsync(b => b.ServiceId == newBooking.ServiceId &&
                               ((newBooking.ScheduledStart >= b.ScheduledStart && newBooking.ScheduledStart < b.ScheduledEnd) ||
                                (newBooking.ScheduledEnd > b.ScheduledStart && newBooking.ScheduledEnd <= b.ScheduledEnd) ||
                                (newBooking.ScheduledStart <= b.ScheduledStart && newBooking.ScheduledEnd >= b.ScheduledEnd)));

            if(bookingConflict)
            {
                return Result<BookingDto?>.Failure("The service is already booked for the requested time.");
            }


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

            // Fetch the complete DTO with user/service info
            return Result<BookingDto?>.Success(
                await GetBookingDtoByIdAsync(booking.Id),
                "Booking created successfully.");
        }

        public async Task<Result<BookingDto?>> UpdateBookingAsync(int bookingId, UpdateBookingDto updatedBooking, ClaimsPrincipal user)
        {
            /*Customers can only update their own bookings
             * Customers cannot update a booking within 24 hours of the scheduled start time
             * Admin and Staff can update any booking
             */

            var existingBooking = await Db.Bookings.FindAsync(bookingId);

            // Check if booking exists
            if (existingBooking == null)
            {
                return Result<BookingDto?>.Failure("Booking not found.");
            }

            var role = user.FindFirst(ClaimTypes.Role)?.Value;
            var userId = int.Parse(user.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            // Check permissions
            if (role == "Customer" && existingBooking.UserId != userId)
            {
                return Result<BookingDto?>.Failure("You do not have permission to update this booking.");
            }

            // Check 24-hour restriction for Customers
            if (role == "Customer" && (existingBooking.ScheduledStart - DateTime.UtcNow).TotalHours < 24)
            {
                return Result<BookingDto?>.Failure("You cannot update a booking within 24 hours of the scheduled start time.");
            }

            // Validate scheduled times
            if (updatedBooking.ScheduledEnd <= updatedBooking.ScheduledStart)
            {
                return Result<BookingDto?>.Failure("Scheduled end time must be after scheduled start time.");
            }

            // Validate scheduled times are in the future
            if (updatedBooking.ScheduledStart < DateTime.UtcNow)
            {
                return Result<BookingDto?>.Failure("Scheduled start time must be in the future.");
            }

            // Validate booking duration (max 8 hours)
            var duration = updatedBooking.ScheduledEnd - updatedBooking.ScheduledStart;
            if (duration.TotalHours > 8)
            {
                return Result<BookingDto?>.Failure("Booking duration cannot exceed 8 hours.");
            }

            // Validate booking status (can only take 4 values)
            if (updatedBooking.Status != "Pending" && updatedBooking.Status != "Confirmed" && updatedBooking.Status != "Cancelled" && updatedBooking.Status != "Completed")
            {
                return Result<BookingDto?>.Failure("Invalid booking status. Allowed values are: Pending, Confirmed, Cancelled, Completed.");
            }

            existingBooking.ScheduledStart = updatedBooking.ScheduledStart;
            existingBooking.ScheduledEnd = updatedBooking.ScheduledEnd;
            existingBooking.Status = updatedBooking.Status;

            await Db.SaveChangesAsync();

            return Result<BookingDto?>.Success(
                await GetBookingDtoByIdAsync(existingBooking.Id),
                "Booking updated successfully.");
        }

        public async Task<bool> DeleteBookingAsync(int bookingId, ClaimsPrincipal user)
        {
            /*Customers can only delete their own bookings
             * Only admins can delete any booking
             * Customers cannot delete a booking within 24 hours of the scheduled start time
             */
            var booking = await Db.Bookings.FindAsync(bookingId);
            
            if (booking == null)
                return false;

            var role = user.FindFirst(ClaimTypes.Role)?.Value;
            var userId = int.Parse(user.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            if(role != "Admin" && booking.UserId != userId)
                throw new UnauthorizedAccessException("You do not have permission to delete this booking.");

            if(role != "Admin" && (booking.ScheduledStart - DateTime.UtcNow).TotalHours < 24)
                throw new InvalidOperationException("You cannot delete a booking within 24 hours of the scheduled start time.");

            Db.Bookings.Remove(booking);
            await Db.SaveChangesAsync();
            return true;
        }
    }
}
