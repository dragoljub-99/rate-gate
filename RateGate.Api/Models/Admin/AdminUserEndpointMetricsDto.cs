namespace RateGate.Api.Models.Admin
{
    public class AdminUserEndpointMetricsDto
    {
        public string Endpoint { get; set; } = null!;

        public int RequestCount { get; set; }

        public DateTime? LastRequestAtUtc { get; set; }
    }
}
