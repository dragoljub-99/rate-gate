using System.ComponentModel.DataAnnotations;
using RateGate.Domain.Entities;

namespace RateGate.Api.Models.Admin
{
    public class AdminPolicyUpdateDto
    {
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = null!;

        [Required]
        [MaxLength(400)]
        public string EndpointPattern { get; set; } = null!;

        [Required]
        public RateLimitAlgorithm Algorithm { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        public int Limit { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        public int WindowInSeconds { get; set; }

        public int? BurstLimit { get; set; }
    }
}
