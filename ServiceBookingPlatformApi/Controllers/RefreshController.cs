using Microsoft.AspNetCore.Mvc;
using ServiceBookingPlatform.Models.Dtos.User;
using ServiceBookingPlatform.Services;

namespace ServiceBookingPlatform.Controllers
{
    [Route("auth/refresh")]
    [ApiController]
    public class RefreshController(IRefreshService refreshService, ILogger<RefreshController> logger) : ControllerBase
    {
        [HttpPost]
        public async Task<ActionResult<UserLogInResponseDto>> Refresh([FromBody] RefreshTokenRequestDto request)
        {
            var result = await refreshService.RefreshTokenAsync(request.RefreshToken);

            if (!result.IsSuccess)
            {
                logger.LogWarning("TokenRefresh: Attempt failed. Reason: {Message}", result.Message);
                return Unauthorized(new { message = result.Message, errors = result.Errors });
            }

            logger.LogInformation("TokenRefresh: Token successfully rotated for {Email}", result.Data!.Email);
            return Ok(result.Data);
        }
    }
}
