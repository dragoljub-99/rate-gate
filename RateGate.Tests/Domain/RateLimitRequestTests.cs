using RateGate.Domain.RateLimiting;
using Xunit;

namespace RateGate.Tests.Domain
{

    public class RateLimitRequestTests
    {
        [Fact]
        public void Constructor_SetsAllProperties_WhenInputIsValid()
        {
            var request = new RateLimitRequest(
                apiKey: "api-key-1",
                endpoint: "/api/orders",
                cost: 2,
                limit: 10,
                windowInSeconds: 60,
                burstLimit: 5);

            Assert.Equal("api-key-1", request.ApiKey);
            Assert.Equal("/api/orders", request.Endpoint);
            Assert.Equal(2, request.Cost);
            Assert.Equal(10, request.Limit);
            Assert.Equal(60, request.WindowInSeconds);
            Assert.Equal(5, request.BurstLimit);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Constructor_Throws_WhenApiKeyIsMissing(string? apiKey)
        {
            Assert.Throws<ArgumentException>(() =>
                new RateLimitRequest(
                    apiKey: apiKey!,
                    endpoint: "/api/orders",
                    cost: 1,
                    limit: 10,
                    windowInSeconds: 60));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Constructor_Throws_WhenEndpointIsMissing(string? endpoint)
        {
            Assert.Throws<ArgumentException>(() =>
                new RateLimitRequest(
                    apiKey: "api-key-1",
                    endpoint: endpoint!,
                    cost: 1,
                    limit: 10,
                    windowInSeconds: 60));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Constructor_Throws_WhenCostIsNotPositive(int cost)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new RateLimitRequest(
                    apiKey: "api-key-1",
                    endpoint: "/api/orders",
                    cost: cost,
                    limit: 10,
                    windowInSeconds: 60));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Constructor_Throws_WhenLimitIsNotPositive(int limit)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new RateLimitRequest(
                    apiKey: "api-key-1",
                    endpoint: "/api/orders",
                    cost: 1,
                    limit: limit,
                    windowInSeconds: 60));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Constructor_Throws_WhenWindowIsNotPositive(int windowInSeconds)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new RateLimitRequest(
                    apiKey: "api-key-1",
                    endpoint: "/api/orders",
                    cost: 1,
                    limit: 10,
                    windowInSeconds: windowInSeconds));
        }
    }
}