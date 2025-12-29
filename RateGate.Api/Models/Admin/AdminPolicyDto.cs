using RateGate.Domain.Entities;

namespace RateGate.Api.Models.Admin
{
    public class AdminPolicyDto
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public string Name { get; set; } = null!;

        public string EndpointPattern { get; set; } = null!;

        public RateLimitAlgorithm Algorithm { get; set; }

        public int Limit { get; set; }

        public int WindowInSeconds { get; set; }

        public int? BurstLimit { get; set; }

        public DateTime CreatedAtUtc { get; set; }
    }
}
