namespace RateGate.Api.Models.Admin
{
    public class AdminUserMetricsDto
    {
        public int UserId { get; set; }

        public string Name { get; set; } = null!;

        public string? Email { get; set; }

        public int ApiKeysCount { get; set; }

        public int PoliciesCount { get; set; }

        public int TotalRequests { get; set; }

        public DateTime? LastRequestAtUtc { get; set; }
    }
}