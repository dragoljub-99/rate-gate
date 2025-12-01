namespace RateGate.Domain.Entities
{
    public class UsageLog
    {
        public long Id { get; set; }
        public int ApiKeyId { get; set; }
        public string Endpoint { get; set; } = null!;
        public DateTime OccurredAtUtc { get; set; }
        public int Cost { get; set; }
        public ApiKey ApiKey { get; set; } = null!;
    }
}
