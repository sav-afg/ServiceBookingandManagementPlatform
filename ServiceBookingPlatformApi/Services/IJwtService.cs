using ServiceBookingPlatform.Models.Dtos.User;

namespace ServiceBookingPlatform.Services
{
    // This service is responsible for handling JWT authentication logic, such as validating user credentials and generating JWT tokens.
    public interface IJwtService
    {
        public Task<UserLogInResponseDto?> Authenticate(UserLogInRequestDto request);
    }
}
