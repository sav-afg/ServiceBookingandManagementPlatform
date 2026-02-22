using ServiceBookingPlatform.Models;

using ServiceBookingPlatform.Services.Common;
using ServiceBookingPlatform.Models.Dtos.User;
namespace ServiceBookingPlatform.Services
{
    public interface IUserRegistrationService
    {
        public Task<bool> EmailExistsAsync(string email);
        public Task<(bool Success, string Message)> RegisterUserAsync(UserDto userDto);

        public Result ValidateUserDto(UserDto userDto);
    }
}
