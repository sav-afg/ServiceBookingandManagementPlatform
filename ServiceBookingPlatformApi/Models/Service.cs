using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace ServiceBookingPlatform.Models
{
    public class Service
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public required string ServiceName { get; set; }

        [Required]
        [MaxLength(50)]
        public required string ServiceType { get; set; }

        [MaxLength(500)]
        public string ServiceDescription { get; set; } = string.Empty;
    }
}
