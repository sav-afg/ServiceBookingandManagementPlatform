using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ServiceBookingPlatform.Data;
using ServiceBookingPlatform.Models.Dtos.User;
using ServiceBookingPlatform.Services;

namespace ServiceBookingPlatform.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [AllowAnonymous]
    public class UserLogInController(IUserLogInService service, IJwtService jwtService) : ControllerBase
    {
        [HttpPost("validate")]
        public async Task<ActionResult> ValidateLogIn(UserLogInRequestDto user)
        {
            var (success, message) = await service.ValidateUserCredentialsAsync(user);
            if (success)
            {
                return Ok(new { message });
            }
            else
            {
                return Unauthorized(new { message });
            }
        }

        [HttpPost]
        public async Task<ActionResult<UserLogInResponseDto>> LogIn(UserLogInRequestDto user)
        {
            var result = await jwtService.Authenticate(user);

            if (result is null)
                return Unauthorized();

            return result;
        }

        [HttpPost("logout")]
        public async Task<ActionResult> LogOut([FromBody] RefreshTokenRequestDto request)
        {
            var (success, message) = await service.LogOutAsync(request.RefreshToken);

            if (!success)
            {
                return BadRequest(new { message });
            }

            return Ok(new { message });
        }
    }
}
