using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceBookingPlatform.Models.Dtos.User;
using ServiceBookingPlatform.Services;

namespace ServiceBookingPlatform.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [AllowAnonymous]
    public class UserRegistrationController(IUserRegistrationService service, ILogger<UserRegistrationController> logger) : ControllerBase
    {
        [HttpPost]
        public async Task<ActionResult> RegisterUser(UserDto userDto)
        {
            var (success, message) = await service.RegisterUserAsync(userDto);

            if (success)
            {
                logger.LogInformation("RegisterUser: New account registered for {Email}", userDto.Email);
                return Ok(new { message });
            }

            logger.LogWarning("RegisterUser: Registration failed for {Email}. Reason: {Message}", userDto.Email, message);
            return BadRequest(new { message });
        }

        [HttpPost("validate")]
        public ActionResult ValidateUser(UserDto userDto)
        {
            var validationResult = service.ValidateUserDto(userDto);

            if (validationResult.IsValid)
            {
                logger.LogDebug("ValidateUser: Validation passed for {Email}", userDto.Email);
                return Ok(validationResult);
            }

            logger.LogDebug("ValidateUser: Validation failed for {Email}", userDto.Email);
            return BadRequest(validationResult);
        }

        [HttpGet("check-email")]
        public async Task<ActionResult> CheckEmailExists([FromQuery] string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                logger.LogDebug("CheckEmailExists: Request received with missing email parameter");
                return BadRequest(new { message = "Email is required" });
            }

            var exists = await service.EmailExistsAsync(email);
            logger.LogDebug("CheckEmailExists: {Email} exists: {Exists}", email, exists);
            return Ok(new { exists });
        }
    }
}
