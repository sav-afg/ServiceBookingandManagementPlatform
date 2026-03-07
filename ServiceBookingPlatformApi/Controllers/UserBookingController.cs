using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceBookingPlatform.Models.Dtos.Booking;
using ServiceBookingPlatform.Services;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
namespace ServiceBookingPlatform.Controllers
{
    [Route("api/[controller]")]
    [ApiController]

    /* Only authenticated users can access these endpoints
     * Customers can manage their own bookings
     * Staff can view all bookings and manage bookings assigned to them
     * Admins have full access to all bookings
     */

    [Authorize]
    public class UserBookingController(IUserBookingService service, ILogger<UserBookingController> logger) : ControllerBase
    {
        private string ActorId => User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "unknown";
        private string ActorName => User.FindFirst(ClaimTypes.Name)?.Value ?? "unknown";
        private string ActorRole => User.FindFirst(ClaimTypes.Role)?.Value ?? "unknown";
        private string ActorEmail => User.FindFirst(ClaimTypes.Email)?.Value ?? "unknown";

        [HttpGet]
        // Authorization is handled by the service layer - customers see only their bookings, staff/admin see all
        public async Task<ActionResult<List<BookingDto>>> GetAllBookings()
        {
            var bookings = await service.GetAllBookingsAsync(User);

            if (bookings.Count == 0)
            {
                logger.LogDebug("GetAllBookings: No bookings found for {ActorName} (ID: {ActorId}) (Role: {ActorRole})", ActorName, ActorId, ActorRole);
                return NotFound("No bookings found.");
            }

            logger.LogDebug("GetAllBookings: Returned {Count} booking(s) to {ActorName} (ID: {ActorId}) (Role: {ActorRole})", bookings.Count, ActorName, ActorId, ActorRole);
            return Ok(bookings);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<BookingDto>> GetBookingById(int id)
        {
            try
            {
                var booking = await service.GetBookingByIdAsync(id, User);

                if (booking is null)
                {
                    logger.LogDebug("GetBookingById: Booking {BookingId} not found, requested by {ActorName} (ID: {ActorId})", id, ActorName, ActorId);
                    return NotFound($"Booking with ID {id} was not found");
                }

                logger.LogDebug("GetBookingById: Booking {BookingId} retrieved by {ActorName} (ID: {ActorId}) (Role: {ActorRole})", id, ActorName, ActorId, ActorRole);
                return Ok(booking);
            }
            catch (UnauthorizedAccessException)
            {
                logger.LogWarning("GetBookingById: Unauthorized access attempt to booking {BookingId} by {ActorName} (ID: {ActorId}) (Role: {ActorRole})", id, ActorName, ActorId, ActorRole);
                return StatusCode(403, new { message = $"You do not have permission to access booking {id}." });
            }
            catch (NullReferenceException ex)
            {
                logger.LogDebug("GetBookingById: Booking {BookingId} not found, requested by {ActorName} (ID: {ActorId}). {Message}", id, ActorName, ActorId, ex.Message);
                return NotFound(ex.Message);
            }
        }

        [HttpPost]
        public async Task<ActionResult<BookingDto>> AddBooking(CreateBookingDto booking)
        {
            // Try to get user ID from JWT claims
            // The JWT nameid claim gets mapped to ClaimTypes.NameIdentifier after authentication
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)
                           ?? User.FindFirst(JwtRegisteredClaimNames.NameId)
                           ?? User.FindFirst("nameid");

            if (userIdClaim == null)
            {
                logger.LogWarning("AddBooking: Missing NameIdentifier claim for {ActorName} ({ActorEmail})", ActorName, ActorEmail);
                return Unauthorized(new { message = "User ID not found in token claims" });
            }

            if (!int.TryParse(userIdClaim.Value, out var userId))
            {
                logger.LogWarning("AddBooking: Invalid user ID format '{ClaimValue}' in token for {ActorName} ({ActorEmail})", userIdClaim.Value, ActorName, ActorEmail);
                return BadRequest(new { message = "Invalid user ID format in token" });
            }

            var result = await service.CreateBookingAsync(userId, booking);

            if (!result.IsSuccess)
            {
                logger.LogWarning("AddBooking: Failed for {ActorName} (ID: {ActorId}). Reason: {Message}", ActorName, ActorId, result.Message);
                return BadRequest(new
                {
                    message = result.Message,
                    errors = result.Errors
                });
            }

            logger.LogInformation("AddBooking: Booking {BookingId} created by {ActorName} (ID: {ActorId}) (Role: {ActorRole}), service {ServiceId}",
                result.Data!.Id, ActorName, ActorId, ActorRole, booking.ServiceId);
            return CreatedAtAction(nameof(GetBookingById), new { id = result.Data!.Id }, result.Data);
        }

        [HttpPatch("{id}")]
        public async Task<ActionResult<BookingDto>> UpdateBooking(int id, UpdateBookingDto booking)
        {
            var result = await service.UpdateBookingAsync(id, booking, User);

            if (!result.IsSuccess)
            {
                logger.LogWarning("UpdateBooking: Failed for booking {BookingId} by {ActorName} (ID: {ActorId}) (Role: {ActorRole}). Reason: {Message}",
                    id, ActorName, ActorId, ActorRole, result.Message);
                return BadRequest(new
                {
                    message = result.Message,
                    errors = result.Errors
                });
            }

            logger.LogInformation("UpdateBooking: Booking {BookingId} updated by {ActorName} (ID: {ActorId}) (Role: {ActorRole})",
                id, ActorName, ActorId, ActorRole);
            return Ok(result.Data);
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteBooking(int id)
        {
            try
            {
                var result = await service.DeleteBookingAsync(id, User);

                if (!result)
                {
                    logger.LogDebug("DeleteBooking: Booking {BookingId} not found, requested by {ActorName} (ID: {ActorId})", id, ActorName, ActorId);
                    return NotFound($"Booking with ID {id} was not found");
                }

                logger.LogInformation("DeleteBooking: Booking {BookingId} deleted by {ActorName} (ID: {ActorId}) (Role: {ActorRole})",
                    id, ActorName, ActorId, ActorRole);
                return Ok($"Booking with ID {id} successfully deleted.");
            }
            catch (NullReferenceException ex)
            {
                logger.LogDebug("DeleteBooking: Booking {BookingId} not found, requested by {ActorName} (ID: {ActorId}). {Message}", id, ActorName, ActorId, ex.Message);
                return NotFound(ex.Message);
            }
            catch (UnauthorizedAccessException)
            {
                logger.LogWarning("DeleteBooking: Unauthorized attempt to delete booking {BookingId} by {ActorName} (ID: {ActorId}) (Role: {ActorRole})",
                    id, ActorName, ActorId, ActorRole);
                return Forbid();
            }
            catch (InvalidOperationException ex)
            {
                logger.LogWarning("DeleteBooking: Business rule violation for booking {BookingId} by {ActorName} (ID: {ActorId}) (Role: {ActorRole}). Reason: {Message}",
                    id, ActorName, ActorId, ActorRole, ex.Message);
                return BadRequest(ex.Message);
            }
        }
    }
}
