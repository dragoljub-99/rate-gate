using System.Collections.Concurrent;

namespace RateGate.Domain.RateLimiting
{
    public class TokenBucketRateLimiter : IRateLimiter
    {
        private readonly ITimeProvider _timeProvider;

        private readonly ConcurrentDictionary<string, TokenBucketState> _buckets = new();

        private sealed class TokenBucketState
        {
            public double Tokens;
            public DateTime LastRefillUtc;
            public readonly object SyncRoot = new();
        }

        public TokenBucketRateLimiter(ITimeProvider timeProvider)
        {
            _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        }

        public Task<RateLimitResult> CheckAsync(
            RateLimitRequest request,
            CancellationToken cancellationToken = default)
        {
            var now = _timeProvider.UtcNow;

            var capacity = request.BurstLimit ?? request.Limit;
            var refillTokensPerSecond = (double)request.Limit / request.WindowInSeconds;

            if (request.Cost > capacity)
            {
                return Task.FromResult(RateLimitResult.Deny(
                                       RateLimitDecisionReason.LimitExceeded,
                                       retryAfterMs: null, 
                                       remaining: null,
                                       message: "Request cost exceeds the token bucket capacity"
                ));
            }

            var bucketKey = $"{request.ApiKey}:{request.Endpoint}";

            var state = _buckets.GetOrAdd(bucketKey, _ =>
                new TokenBucketState
                {
                    Tokens = capacity,
                    LastRefillUtc = now
                });

            RateLimitResult result;

            lock (state.SyncRoot)
            {
                var elapsedSeconds = (now - state.LastRefillUtc).TotalSeconds;

                if (elapsedSeconds > 0)
                {

                    if (refillTokensPerSecond > 0)
                    {
                        var refill = elapsedSeconds * refillTokensPerSecond;
                        state.Tokens = Math.Min(capacity, state.Tokens + refill);
                    }

                    state.LastRefillUtc = now;
                }

                if (state.Tokens >= request.Cost)
                {
                    state.Tokens -= request.Cost;
                    var remainingApprox = (int)Math.Floor(state.Tokens);

                    result = RateLimitResult.Allow(
                        remaining: remainingApprox,
                        message: "Request allowed by token bucket.");
                }
                else
                {
                    var missingTokens = request.Cost - state.Tokens;
                    int? retryAfterMs = null;

                        var secondsToWait = missingTokens / refillTokensPerSecond;
                        retryAfterMs = (int)Math.Ceiling(secondsToWait * 1000);
                    

                    var remainingApprox = (int)Math.Floor(Math.Max(0, state.Tokens));

                    result = RateLimitResult.Deny(
                        RateLimitDecisionReason.LimitExceeded,
                        retryAfterMs: retryAfterMs,
                        remaining: remainingApprox,
                        message: "Token bucket limit exceeded.");
                }
            }

            return Task.FromResult(result);
        }
    }
}