using System.ComponentModel.DataAnnotations;

namespace RateGate.Api.Models
{
    public class RateLimitCheckRequestDto
    {
        [Required]
        public string ApiKey { get; set; } = null!;

        [Required]
        public string Endpoint { get; set; } = null!;
        
        [Range(1, int.MaxValue)]
        public int? Cost { get; set; }
    }
}