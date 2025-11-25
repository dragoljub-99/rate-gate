
namespace RateGate.Domain.RateLimiting
{
    public interface ITimeProvider
    {
        DateTime UtcNow { get; }
    }
}