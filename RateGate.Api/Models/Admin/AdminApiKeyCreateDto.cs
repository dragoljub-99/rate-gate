using System.ComponentModel.DataAnnotations;

namespace RateGate.Api.Models.Admin
{
    public class AdminApiKeyCreateDto
    {
        [Required]
        public int UserId { get; set; }
        [MaxLength(128)]
        public string? Key { get; set; }

        public bool? IsActive { get; set; }
    }
}