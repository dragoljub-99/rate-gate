using System.Data;
using Microsoft.EntityFrameworkCore;
using RateGate.Domain.Entities;
using RateGate.Domain.RateLimiting;
using RateGate.Infrastructure.Data;

namespace RateGate.Infrastructure.RateLimiting
{
    public class SlidingWindowLogRateLimiter : IRateLimiter
    {
        private readonly RateGateDbContext _dbContext;
        private readonly ITimeProvider _timeProvider;

        private const int MaxRetries = 3;

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
            for (int attempt = 1; attempt <= MaxRetries; attempt++)
            {
                try
                {
                    var now = _timeProvider.UtcNow;
                    var windowStart = now.AddSeconds(-request.WindowInSeconds);

                    await using var transaction =
                        await _dbContext.Database.BeginTransactionAsync(
                            IsolationLevel.Serializable,
                            cancellationToken);

                    var apiKey = await _dbContext.ApiKeys
                        .FirstOrDefaultAsync(k => k.Key == request.ApiKey, cancellationToken);

                    if (apiKey == null || !apiKey.IsActive)
                    {
                        await transaction.RollbackAsync(cancellationToken);

                        return RateLimitResult.Deny(
                            RateLimitDecisionReason.ApiKeyInvalidOrInactive,
                            message: "API key invalid or inactive.");
                    }

                    var used = await _dbContext.UsageLogs
                        .Where(l =>
                            l.ApiKeyId == apiKey.Id &&
                            l.Endpoint == request.Endpoint &&
                            l.OccurredAtUtc >= windowStart)
                        .SumAsync(l => (int?)l.Cost ?? 0, cancellationToken);

                    var totalIfAllowed = used + request.Cost;

                    if (totalIfAllowed > request.Limit)
                    {
                        var oldestInWindow = await _dbContext.UsageLogs
                            .Where(l =>
                                l.ApiKeyId == apiKey.Id &&
                                l.Endpoint == request.Endpoint &&
                                l.OccurredAtUtc >= windowStart)
                            .OrderBy(l => l.OccurredAtUtc)
                            .FirstOrDefaultAsync(cancellationToken);

                        int? retryAfterMs = null;

                        if (oldestInWindow != null)
                        {
                            var expiry = oldestInWindow.OccurredAtUtc
                                .AddSeconds(request.WindowInSeconds);

                            var wait = expiry - now;
                            if (wait > TimeSpan.Zero)
                                retryAfterMs = (int)Math.Ceiling(wait.TotalMilliseconds);
                        }

                        await transaction.CommitAsync(cancellationToken);

                        return RateLimitResult.Deny(
                            RateLimitDecisionReason.LimitExceeded,
                            retryAfterMs,
                            request.Limit - used,
                            "Sliding window limit exceeded.");
                    }

                    _dbContext.UsageLogs.Add(new UsageLog
                    {
                        ApiKeyId = apiKey.Id,
                        Endpoint = request.Endpoint,
                        OccurredAtUtc = now,
                        Cost = request.Cost
                    });

                    await _dbContext.SaveChangesAsync(cancellationToken);

                    await transaction.CommitAsync(cancellationToken);

                    return RateLimitResult.Allow(
                        request.Limit - totalIfAllowed,
                        "Request allowed by sliding window.");
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (DbUpdateException) when (attempt < MaxRetries)
                {
                    _dbContext.ChangeTracker.Clear();
                    continue;
                }
                catch (Exception ex)
                {
                    return RateLimitResult.Deny(
                        RateLimitDecisionReason.InternalError,
                        message: $"Sliding window failed: {ex.Message}");
                }
            }

            return RateLimitResult.Deny(
                RateLimitDecisionReason.InternalError,
                message: "Sliding window failed after retries.");
        }
    }
}