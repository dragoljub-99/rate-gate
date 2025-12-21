using Microsoft.EntityFrameworkCore;
using RateGate.Domain.RateLimiting;
using RateGate.Infrastructure.Data;

namespace RateGate.Infrastructure.RateLimiting
{
    public class SlidingWindowLogRateLimiter : IRateLimiter
    {
        private readonly RateGateDbContext _dbContext;
        private readonly ITimeProvider _timeProvider;

        public SlidingWindowLogRateLimiter(
            RateGateDbContext dbContext,
            ITimeProvider timeProvider)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        }

        public async Task<RateLimitResult> CheckAsync(
            RateLimitRequest request,
            CancellationToken cancellationToken = default)
        {
            var now = _timeProvider.UtcNow;

            try
            {
                var apiKeyEntity = await _dbContext.ApiKeys
                    .FirstOrDefaultAsync(
                        k => k.Key == request.ApiKey,
                        cancellationToken);

                if (apiKeyEntity is null || !apiKeyEntity.IsActive)
                {
                    return RateLimitResult.Deny(
                        RateLimitDecisionReason.ApiKeyInvalidOrInactive,
                        message: "API key is invalid or inactive (sliding window).");
                }

                var apiKeyId = apiKeyEntity.Id;

                var windowStart = now.AddSeconds(-request.WindowInSeconds);

                var usedCost = await _dbContext.UsageLogs
                    .Where(l =>
                        l.ApiKeyId == apiKeyId &&
                        l.Endpoint == request.Endpoint &&
                        l.OccurredAtUtc >= windowStart)
                    .SumAsync(l => (int?)l.Cost, cancellationToken);

                var used = usedCost ?? 0;

                var totalIfAllowed = used + request.Cost;
                if (totalIfAllowed > request.Limit)
                {
                    var oldestInWindow = await _dbContext.UsageLogs
                        .Where(l =>
                            l.ApiKeyId == apiKeyId &&
                            l.Endpoint == request.Endpoint &&
                            l.OccurredAtUtc >= windowStart)
                        .OrderBy(l => l.OccurredAtUtc)
                        .FirstOrDefaultAsync(cancellationToken);

                    int? retryAfterMs = null;

                    if (oldestInWindow != null)
                    {
                        var expiry = oldestInWindow.OccurredAtUtc.AddSeconds(request.WindowInSeconds);
                        var wait = expiry - now;
                        if (wait > TimeSpan.Zero)
                        {
                            retryAfterMs = (int)Math.Ceiling(wait.TotalMilliseconds);
                        }
                    }

                    var remaining = Math.Max(0, request.Limit - used);

                    return RateLimitResult.Deny(
                        RateLimitDecisionReason.LimitExceeded,
                        retryAfterMs: retryAfterMs,
                        remaining: remaining,
                        message: "Sliding window limit exceeded.");
                }

                var log = new Domain.Entities.UsageLog
                {
                    ApiKeyId = apiKeyId,
                    Endpoint = request.Endpoint,
                    OccurredAtUtc = now,
                    Cost = request.Cost
                };

                _dbContext.UsageLogs.Add(log);
                await _dbContext.SaveChangesAsync(cancellationToken);

                var remainingAfter = request.Limit - totalIfAllowed;

                return RateLimitResult.Allow(
                    remaining: remainingAfter,
                    message: "Request allowed by sliding window log.");
            }
            catch (Exception ex)
            {
                return RateLimitResult.Deny(
                    RateLimitDecisionReason.InternalError,
                    message: $"Sliding window evaluation failed: {ex.Message}");
            }
        }
    }
}