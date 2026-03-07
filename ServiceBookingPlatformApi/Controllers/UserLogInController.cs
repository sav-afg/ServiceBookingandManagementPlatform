using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceBookingPlatform.Models.Dtos.User;
using ServiceBookingPlatform.Services;

namespace ServiceBookingPlatform.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [AllowAnonymous]
    public class UserLogInController(IUserLogInService service, IJwtService jwtService, ILogger<UserLogInController> logger) : ControllerBase
    {
        [HttpPost("validate")]
        public async Task<ActionResult> ValidateLogIn(UserLogInRequestDto user)
        {
            var (success, message) = await service.ValidateUserCredentialsAsync(user);

            if (success)
            {
                logger.LogDebug("ValidateLogIn: Credentials valid for {Email}", user.Email);
                return Ok(new { message });
            }

            logger.LogDebug("ValidateLogIn: Credentials invalid for {Email}. Reason: {Message}", user.Email, message);
            return Unauthorized(new { message });
        }

        [HttpPost]
        public async Task<ActionResult<UserLogInResponseDto>> LogIn(UserLogInRequestDto user)
        {
            var result = await jwtService.Authenticate(user);

            if (result is null)
            {
                logger.LogWarning("LogIn: Failed attempt for {Email}", user.Email);
                return Unauthorized();
            }

            logger.LogInformation("LogIn: {Email} authenticated successfully", user.Email);
            return result;
        }

        [HttpPost("logout")]
        public async Task<ActionResult> LogOut([FromBody] RefreshTokenRequestDto request)
        {
            var (success, message) = await service.LogOutAsync(request.RefreshToken);

            if (!success)
            {
                logger.LogWarning("LogOut: Failed. Reason: {Message}", message);
                return BadRequest(new { message });
            }

            logger.LogInformation("LogOut: Session terminated successfully");
            return Ok(new { message });
        }
    }
}
