using System.Collections.Concurrent;

namespace RateGate.Domain.RateLimiting
{
    public class TokenBucketRateLimiter : IRateLimiter
    {
        private readonly int _capacity;
        private readonly double _refillTokensPerSecond;
        private readonly ITimeProvider _timeProvider;

        private readonly ConcurrentDictionary<string, TokenBucketState> _buckets = new();

        private sealed class TokenBucketState
        {
            public double Tokens;
            public DateTime LastRefillUtc;
            public readonly object SyncRoot = new();
        }

        public TokenBucketRateLimiter(int capacity, int windowInSeconds, ITimeProvider timeProvider)
        {
            if (capacity <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity must be positive.");
            }

            if (windowInSeconds <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(windowInSeconds), "Window must be positive.");
            }

            _capacity = capacity;
            _refillTokensPerSecond = (double)capacity / windowInSeconds;
            _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        }

        public Task<RateLimitResult> CheckAsync(
            RateLimitRequest request,
            CancellationToken cancellationToken = default)
        {

            var now = _timeProvider.UtcNow;

            var bucketKey = $"{request.ApiKey}:{request.Endpoint}";

            var state = _buckets.GetOrAdd(bucketKey, _ =>
                new TokenBucketState
                {
                    Tokens = _capacity,
                    LastRefillUtc = now
                });

            RateLimitResult result;

            lock (state.SyncRoot)
            {
                var elapsedSeconds = (now - state.LastRefillUtc).TotalSeconds;
                if (elapsedSeconds > 0 && _refillTokensPerSecond > 0)
                {
                    var refill = elapsedSeconds * _refillTokensPerSecond;
                    state.Tokens = Math.Min(_capacity, state.Tokens + refill);
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

                    if (_refillTokensPerSecond > 0)
                    {
                        var secondsToWait = missingTokens / _refillTokensPerSecond;
                        retryAfterMs = (int)Math.Ceiling(secondsToWait * 1000);
                    }

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
