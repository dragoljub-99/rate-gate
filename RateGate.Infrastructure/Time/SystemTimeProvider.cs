using RateGate.Domain.RateLimiting;

namespace RateGate.Infrastructure.Time
{
    public sealed class SystemTimeProvider : ITimeProvider
    {
        public DateTime UtcNow => DateTime.UtcNow;
    }
}