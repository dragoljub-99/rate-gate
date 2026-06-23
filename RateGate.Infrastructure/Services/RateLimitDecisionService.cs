using RateGate.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using RateGate.Domain.RateLimiting;
using RateGate.Infrastructure.Data;
using RateGate.Domain.Abstractions;
using RateGate.Infrastructure.RateLimiting;

namespace RateGate.Infrastructure.Services
{
    public class RateLimitDecisionService : IRateLimitDecisionService
    {
        private readonly RateGateDbContext _dbContext;
        private readonly TokenBucketRateLimiter _tokenBucket;
        private readonly SlidingWindowLogRateLimiter _slidingWindow;

        public RateLimitDecisionService(RateGateDbContext dbContext,
                                        TokenBucketRateLimiter tokenBucket, 
                                        SlidingWindowLogRateLimiter slidingWindow)
        {
            _dbContext = dbContext;
            _tokenBucket = tokenBucket;
            _slidingWindow = slidingWindow;
        }

        public async Task<RateLimitResult> EvaluateAsync(string apiKey, string endpoint,
                                                         int? cost, CancellationToken cancellationToken)
        {
            var apiKeyEntity = await _dbContext.ApiKeys.AsNoTracking()
                    .Include(k => k.User)
                    .FirstOrDefaultAsync(
                        k => k.Key == apiKey,
                        cancellationToken);

            if (apiKeyEntity == null || !apiKeyEntity.IsActive)
            {
                var invalidResult = RateLimitResult.Deny(RateLimitDecisionReason.ApiKeyInvalidOrInactive,
                                                        message: "API key is invalid or inactive");

                return invalidResult;
            }

            var user = apiKeyEntity.User;

            var policies = await _dbContext.Policies.AsNoTracking()
                           .Where(p => p.UserId == user.Id)
                           .ToListAsync(cancellationToken);

            var policy = FindBestMatchingPolicy(policies, endpoint);

            if (policy is null)
            {
                var noPolicyResult = RateLimitResult.Deny(RateLimitDecisionReason.NoMatchingPolicy,
                                                          message: "No matching rate limit policy for this endpoint");
                
                return noPolicyResult;
            }

            var requestCost = cost ?? 1;

           var rlRequest = new RateLimitRequest(
                    apiKey: apiKey,
                    endpoint: endpoint,
                    cost: requestCost,
                    limit: policy.Limit,
                    windowInSeconds: policy.WindowInSeconds,
                    burstLimit: policy.BurstLimit);


            RateLimitResult rlResult;

            switch (policy.Algorithm)
            {
                case RateLimitAlgorithm.TokenBucket:
                     rlResult = await _tokenBucket.CheckAsync(rlRequest, cancellationToken);
                     break;
                
                case RateLimitAlgorithm.SlidingWindowLog:
                     rlResult = await _slidingWindow.CheckAsync(rlRequest, cancellationToken);
                     break;

                default:
                     rlResult = RateLimitResult.Deny(RateLimitDecisionReason.InternalError,
                     message: $"Rate limit algorithm '{policy.Algorithm}' is not supported.");
                     break; 
            }

            return rlResult;
        }

        private static Policy? FindBestMatchingPolicy(IEnumerable<Policy> policies, string endpoint)
        {
            Policy? wildcardMatch = null;
            Policy? prefixMatch = null;
            Policy? exactMatch = null;

            foreach (var policy in policies)
            {
                var pattern = policy.EndpointPattern;

                if (pattern == "*")
                {
                    wildcardMatch ??= policy;
                    continue;
                }

                if (pattern.EndsWith("/*", StringComparison.Ordinal))
                {
                    var prefix = pattern.Substring(0, pattern.Length - 1); 
                    if (endpoint.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    {
                        prefixMatch ??= policy;
                    }

                    continue;
                }

                if (string.Equals(pattern, endpoint, StringComparison.OrdinalIgnoreCase))
                {
                    exactMatch ??= policy;
                }
            }

            return exactMatch ?? prefixMatch ?? wildcardMatch;
        }
    }
}