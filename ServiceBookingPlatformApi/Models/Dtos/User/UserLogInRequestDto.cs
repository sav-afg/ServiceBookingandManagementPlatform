using System.ComponentModel.DataAnnotations;

namespace ServiceBookingPlatform.Models.Dtos.User
{
    public record UserLogInRequestDto
    {
        [Required(ErrorMessage = "Email is required to log in")]
        public required string Email { get; init; }

        [Required(ErrorMessage = "Password is required to log in")]
        public required string Password { get; init; }
    }
}
