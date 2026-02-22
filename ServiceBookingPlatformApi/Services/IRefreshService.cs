using ServiceBookingPlatform.Models;
using ServiceBookingPlatform.Models.Dtos.User;
using ServiceBookingPlatform.Services.Common;

namespace ServiceBookingPlatform.Services
{
    public interface IRefreshService
    {
        public Task<Result<RefreshToken>> GetRefreshToken(UserLogInResponseDto user);

        public Task<Result<UserLogInResponseDto>> RefreshTokenAsync(string refreshToken);
    }
}
