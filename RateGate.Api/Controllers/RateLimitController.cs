using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RateGate.Api.Models;
using RateGate.Domain.Entities;
using RateGate.Domain.RateLimiting;
using RateGate.Infrastructure.Data;
using RateGate.Infrastructure.RateLimiting;

namespace RateGate.Api.Controllers
{
    [ApiController]
    [Route("check")]
    public class RateLimitController : ControllerBase
    {
        private readonly RateGateDbContext _dbContext;
        private readonly TokenBucketRateLimiter _tokenBucketLimiter;
        private readonly SlidingWindowLogRateLimiter _slidingWindowLimiter;

        public RateLimitController(
            RateGateDbContext dbContext,
            TokenBucketRateLimiter tokenBucketLimiter,
            SlidingWindowLogRateLimiter slidingWindowLimiter)
        {
            _dbContext = dbContext;
            _tokenBucketLimiter = tokenBucketLimiter;
            _slidingWindowLimiter = slidingWindowLimiter;
        }

        [HttpPost]
        public async Task<ActionResult<RateLimitCheckResponseDto>> Check(
            [FromBody] RateLimitCheckRequestDto requestDto,
            CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (string.IsNullOrWhiteSpace(requestDto.ApiKey) ||
                string.IsNullOrWhiteSpace(requestDto.Endpoint))
            {
                return BadRequest("ApiKey and Endpoint are required.");
            }

            try
            {
                var apiKeyEntity = await _dbContext.ApiKeys
                    .Include(k => k.User)
                    .FirstOrDefaultAsync(
                        k => k.Key == requestDto.ApiKey,
                        cancellationToken);

                if (apiKeyEntity == null || !apiKeyEntity.IsActive)
                {
                    var invalidResult = RateLimitResult.Deny(
                        RateLimitDecisionReason.ApiKeyInvalidOrInactive,
                        message: "API key is invalid or inactive.");

                    return Ok(RateLimitCheckResponseDto.FromDomain(invalidResult));
                }

                var user = apiKeyEntity.User;

                var policies = await _dbContext.Policies
                    .Where(p => p.UserId == user.Id)
                    .ToListAsync(cancellationToken);

                var policy = FindBestMatchingPolicy(
                    policies,
                    requestDto.Endpoint);

                if (policy == null)
                {
                    var noPolicyResult = RateLimitResult.Deny(
                        RateLimitDecisionReason.NoMatchingPolicy,
                        message: "No matching rate limit policy found for this endpoint.");

                    return Ok(RateLimitCheckResponseDto.FromDomain(noPolicyResult));
                }

                var cost = requestDto.Cost ?? 1;

                var rlRequest = new RateLimitRequest(
                    apiKey: requestDto.ApiKey,
                    endpoint: requestDto.Endpoint,
                    cost: cost,
                    limit: policy.Limit,
                    windowInSeconds: policy.WindowInSeconds,
                    burstLimit: policy.BurstLimit);

                RateLimitResult rlResult;

                switch (policy.Algorithm)
                {
                    case RateLimitAlgorithm.TokenBucket:
                        rlResult = await _tokenBucketLimiter.CheckAsync(rlRequest, cancellationToken);
                        break;

                    case RateLimitAlgorithm.SlidingWindowLog:
                        rlResult = await _slidingWindowLimiter.CheckAsync(rlRequest, cancellationToken);
                        break;

                    default:
                        rlResult = RateLimitResult.Deny(
                            RateLimitDecisionReason.InternalError,
                            message: $"Rate limit algorithm '{policy.Algorithm}' is not supported.");
                        break;
                }

                var responseDto = RateLimitCheckResponseDto.FromDomain(rlResult);

                return Ok(responseDto);
            }
            catch (Exception ex)
            {
                var result = RateLimitResult.Deny(
                    RateLimitDecisionReason.InternalError,
                    message: $"An internal error occurred while evaluating the rate limit: {ex.Message}");

                return StatusCode(500, RateLimitCheckResponseDto.FromDomain(result));
            }
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