using RateGate.Domain.RateLimiting;

namespace RateGate.Tests
{

    internal sealed class MutableTimeProvider : ITimeProvider
    {
        public DateTime UtcNow { get; set; }
    }
}