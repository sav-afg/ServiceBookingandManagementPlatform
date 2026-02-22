using System.ComponentModel.DataAnnotations;

namespace ServiceBookingPlatform.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }
        public required string FirstName { get; set; }
        public string Role { get; set; } = "Customer";
        public required string LastName { get; set; }
        public required string Email { get; set; }
        public required string PasswordHash { get; set; }
        public required string PhoneNumber { get; set; }


    }
}
