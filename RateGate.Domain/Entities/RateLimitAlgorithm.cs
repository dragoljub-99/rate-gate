
namespace RateGate.Domain.Entities
{
    public enum RateLimitAlgorithm
    {
        TokenBucket = 1,
        SlidingWindowLog = 2
    }
}