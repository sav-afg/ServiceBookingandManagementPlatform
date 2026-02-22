using System.ComponentModel.DataAnnotations;

namespace ServiceBookingPlatform.Models
{
    public class RefreshToken
    {
        [Key]
        public int Id { get; set; }

        public int UserId { get; set; }
        public User? User { get; set; }

        public string Token { get; set; } = string.Empty;

        public DateTime ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; }

        public bool IsRevoked { get; set; }
    }
}
