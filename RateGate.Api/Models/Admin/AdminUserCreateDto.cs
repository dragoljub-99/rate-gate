using System.ComponentModel.DataAnnotations;

namespace RateGate.Api.Models.Admin
{
    public class AdminUserCreateDto
    {
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = null!;

        [MaxLength(320)]
        [EmailAddress]
        public string? Email { get; set; }

        [MaxLength(100)]
        public string? Plan { get; set; }
    }
}
