using RateGate.Domain.RateLimiting;

namespace RateGate.Domain.Abstractions
{
    public interface IRateLimitDecisionService
    {
        Task<RateLimitResult> EvaluateAsync(string apiKey, string endpoint, int? cost, CancellationToken ct);
    }
}