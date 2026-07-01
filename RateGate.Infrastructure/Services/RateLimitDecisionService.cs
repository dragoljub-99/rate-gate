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
        private readonly IPolicyResolver _policyResolver;
        private readonly ITimeProvider _timeProvider;

        public RateLimitDecisionService(RateGateDbContext dbContext,
                                        TokenBucketRateLimiter tokenBucket, 
                                        SlidingWindowLogRateLimiter slidingWindow,
                                        IPolicyResolver policyResolver,
                                        ITimeProvider timeProvider)
        {
            _dbContext = dbContext;
            _tokenBucket = tokenBucket;
            _slidingWindow = slidingWindow;
            _policyResolver = policyResolver;
            _timeProvider = timeProvider;
        }

        public async Task<RateLimitResult> EvaluateAsync(string apiKey, string endpoint,
                                                         int? cost, CancellationToken cancellationToken)
        {
            var apiKeyEntity = await _dbContext.ApiKeys
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

            apiKeyEntity.LastUsedAtUtc = _timeProvider.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
            
            var user = apiKeyEntity.User;

            var policies = await _dbContext.Policies.AsNoTracking()
                           .Where(p => p.UserId == user.Id)
                           .ToListAsync(cancellationToken);

            var policy = _policyResolver.FindBestMatch(policies, endpoint);

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


            RateLimitResult rlResult = await ExecuteLimiterAsync(rlRequest, policy.Algorithm, cancellationToken);


            return rlResult;
        }

        private async Task<RateLimitResult> ExecuteLimiterAsync(RateLimitRequest rlRequest, RateLimitAlgorithm algorithm,
                                                               CancellationToken cancellationToken)
        {
            RateLimitResult rlResult;

            switch (algorithm)
            {
                case RateLimitAlgorithm.TokenBucket:
                     rlResult = await _tokenBucket.CheckAsync(rlRequest, cancellationToken);
                     break;
                case RateLimitAlgorithm.SlidingWindowLog:
                     rlResult = await _slidingWindow.CheckAsync(rlRequest, cancellationToken);
                     break;
                default:
                    rlResult = RateLimitResult.Deny(RateLimitDecisionReason.InternalError,
                    message: $"Rate limit algorithm {algorithm} is not suported");
                    break;
            }

            return rlResult;
        }
    }
}