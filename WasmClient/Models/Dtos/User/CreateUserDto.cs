using System.ComponentModel.DataAnnotations;

namespace ServiceBookingPlatform.Models.Dtos.User
{
    public record CreateUserDto
    {
        [Required(ErrorMessage = "First name is required")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "First name must be between 2 and 100 characters")]
        public required string FirstName { get; init; }

        [Required(ErrorMessage = "Role is required")]
        public string Role { get; init; } = "Customer";

        [Required(ErrorMessage = "Last name is required")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Last name must be between 2 and 100 characters")]
        public required string LastName { get; init; }

        [Required(ErrorMessage = "Email is required")]
        public required string Email { get; init; }

        [Required(ErrorMessage = "Password hash is required")]
        public required string PasswordHash { get; init; }

        [Required(ErrorMessage = "Phone number is required")]
        public required string PhoneNumber { get; init; }
    }
}
