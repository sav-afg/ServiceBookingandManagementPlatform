using Microsoft.AspNetCore.Mvc;
using ServiceBookingPlatform.Models.Dtos.User;
using ServiceBookingPlatform.Services;

namespace ServiceBookingPlatform.Controllers
{
    [Route("auth/refresh")]
    [ApiController]
    public class RefreshController(IRefreshService refreshService) : ControllerBase
    {
        [HttpPost]
        public async Task<ActionResult<UserLogInResponseDto>> Refresh([FromBody] RefreshTokenRequestDto request)
        {
            var result = await refreshService.RefreshTokenAsync(request.RefreshToken);

            if (!result.IsSuccess)
            {
                return Unauthorized(new { message = result.Message, errors = result.Errors });
            }

            return Ok(result.Data);
        }
    }
}
