using FieldValidatorAPI;
using ServiceBookingPlatform.Models.Dtos.User;

namespace ServiceBookingPlatform.Services
{
    public interface IUserLogInService
    {
        Task<(bool success, string errors)> ValidateUserCredentialsAsync(UserLogInRequestDto userDto);
        public ValidationResult ValidateUserDto(UserLogInRequestDto user);
        Task<(bool success, string message)> LogOutAsync(string refreshToken);
    }
}
