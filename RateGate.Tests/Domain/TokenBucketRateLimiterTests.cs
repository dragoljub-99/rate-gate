using RateGate.Domain.RateLimiting;
using Xunit;

namespace RateGate.Tests.Domain
{

    public class TokenBucketRateLimiterTests
    {
        private readonly MutableTimeProvider _timeProvider = new()
        {
            UtcNow = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc)
        };

        [Fact]
        public async Task CheckAsync_AllowsUntilBurstCapacityIsSpent_ThenDenies()
        {
            var limiter = new TokenBucketRateLimiter(_timeProvider);

            var first = await limiter.CheckAsync(CreateRequest(cost: 3));
            var second = await limiter.CheckAsync(CreateRequest(cost: 2));
            var third = await limiter.CheckAsync(CreateRequest(cost: 1));

            Assert.True(first.IsAllowed);
            Assert.Equal(2, first.Remaining);

            Assert.True(second.IsAllowed);
            Assert.Equal(0, second.Remaining);

            Assert.False(third.IsAllowed);
            Assert.Equal(RateLimitDecisionReason.LimitExceeded, third.Reason);
            Assert.Equal(0, third.Remaining);
            Assert.Equal(1000, third.RetryAfterMs);
        }

        [Fact]
        public async Task CheckAsync_RefillsTokensBasedOnElapsedTime()
        {
            var limiter = new TokenBucketRateLimiter(_timeProvider);

            var first = await limiter.CheckAsync(CreateRequest(cost: 5));

            _timeProvider.UtcNow = _timeProvider.UtcNow.AddSeconds(2);

            var second = await limiter.CheckAsync(CreateRequest(cost: 2));

            Assert.True(first.IsAllowed);
            Assert.Equal(0, first.Remaining);

            Assert.True(second.IsAllowed);
            Assert.Equal(0, second.Remaining);
        }

        [Fact]
        public async Task CheckAsync_DoesNotRefillAboveBucketCapacity()
        {
            var limiter = new TokenBucketRateLimiter(_timeProvider);

            await limiter.CheckAsync(CreateRequest(cost: 1));

            _timeProvider.UtcNow = _timeProvider.UtcNow.AddSeconds(100);

            var result = await limiter.CheckAsync(CreateRequest(cost: 5));

            Assert.True(result.IsAllowed);
            Assert.Equal(0, result.Remaining);
        }

        [Fact]
        public async Task CheckAsync_UsesSeparateBuckets_ForDifferentEndpoints()
        {
            var limiter = new TokenBucketRateLimiter(_timeProvider);

            var ordersResult = await limiter.CheckAsync(
                CreateRequest(endpoint: "/api/orders", cost: 5));

            var usersResult = await limiter.CheckAsync(
                CreateRequest(endpoint: "/api/users", cost: 5));

            Assert.True(ordersResult.IsAllowed);
            Assert.True(usersResult.IsAllowed);

            Assert.Equal(0, ordersResult.Remaining);
            Assert.Equal(0, usersResult.Remaining);
        }

        [Fact]
        public async Task CheckAsync_Denies_WhenCostExceedsBucketCapacity()
        {
            var limiter = new TokenBucketRateLimiter(_timeProvider);

            var result = await limiter.CheckAsync(CreateRequest(cost: 6));

            Assert.False(result.IsAllowed);
            Assert.Equal(RateLimitDecisionReason.LimitExceeded, result.Reason);
            Assert.Null(result.Remaining);
            Assert.Null(result.RetryAfterMs);
        }

        private static RateLimitRequest CreateRequest(
            string endpoint = "/api/orders",
            int cost = 1)
        {
            return new RateLimitRequest(
                apiKey: "api-key-1",
                endpoint: endpoint,
                cost: cost,
                limit: 10,
                windowInSeconds: 10,
                burstLimit: 5);
        }
    }
}