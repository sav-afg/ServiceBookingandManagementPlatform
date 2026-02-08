using System.ComponentModel.DataAnnotations;

namespace ServiceBookingPlatform.Models.Dtos.User
{
    public class UserLogInRequestDto
    {
        [Required(ErrorMessage = "Email is required to log in")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required to log in")]
        public string Password { get; set; } = string.Empty;
    }
}

