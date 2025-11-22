namespace RateGate.Domain.Entities
{
    public class Policy
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

        public User User { get; set; } = null!;
    }
}
