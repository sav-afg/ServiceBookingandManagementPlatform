using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ServiceBookingPlatform.Models.Dtos.User;
using ServiceBookingPlatform.Services;

namespace ServiceBookingPlatform.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [AllowAnonymous]
    public class UserRegistrationController(IUserRegistrationService service) : ControllerBase
    {
        [HttpPost]
        public async Task<ActionResult> RegisterUser(UserDto userDto)
        {
            var(success, message) = await service.RegisterUserAsync(userDto);

            if (success)
            {
                return Ok(new { message });
            }
            else
            {
                return BadRequest(new { message });
            }

        }

        [HttpPost("validate")]
        public ActionResult ValidateUser(UserDto userDto)
        {
            var validationResult = service.ValidateUserDto(userDto);
            if (validationResult.IsValid)
            {
                return Ok(validationResult);
            }
            else
            {
                return BadRequest(validationResult);
            }
        }


        [HttpGet("check-email")]
        public async Task<ActionResult> CheckEmailExists([FromQuery] string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return BadRequest(new { message = "Email is required" });
            }

            var exists = await service.EmailExistsAsync(email);
            return Ok(new { exists });
        }

    }
}
