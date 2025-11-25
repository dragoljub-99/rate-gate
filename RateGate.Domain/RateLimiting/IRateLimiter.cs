namespace RateGate.Domain.RateLimiting
{
    public interface IRateLimiter
    {
        Task<RateLimitResult> CheckAsync(
            RateLimitRequest request,
            CancellationToken cancellationToken = default);
    }
}