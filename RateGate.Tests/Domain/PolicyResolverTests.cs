using RateGate.Domain.Entities;
using RateGate.Domain.Services;
using Xunit;

namespace RateGate.Tests.Domain
{

    public class PolicyResolverTests
    {
        private readonly PolicyResolver _resolver = new();

        [Fact]
        public void FindBestMatch_ReturnsExactMatch_OverPrefixAndWildcard()
        {
            var exact = CreatePolicy(1, "/api/orders/123");
            var prefix = CreatePolicy(2, "/api/orders/*");
            var wildcard = CreatePolicy(3, "*");

            var result = _resolver.FindBestMatch(
                new[] { wildcard, prefix, exact },
                "/API/ORDERS/123");

            Assert.Same(exact, result);
        }

        [Fact]
        public void FindBestMatch_ReturnsLongestPrefix_WhenMultiplePrefixesMatch()
        {
            var shortPrefix = CreatePolicy(1, "/api/*");
            var longPrefix = CreatePolicy(2, "/api/orders/*");
            var wildcard = CreatePolicy(3, "*");

            var result = _resolver.FindBestMatch(
                new[] { wildcard, shortPrefix, longPrefix },
                "/api/orders/123");

            Assert.Same(longPrefix, result);
        }

        [Fact]
        public void FindBestMatch_ReturnsWildcard_WhenNoExactOrPrefixMatchExists()
        {
            var wildcard = CreatePolicy(1, "*");
            var unrelatedPrefix = CreatePolicy(2, "/admin/*");

            var result = _resolver.FindBestMatch(
                new[] { unrelatedPrefix, wildcard },
                "/api/orders");

            Assert.Same(wildcard, result);
        }

        [Fact]
        public void FindBestMatch_ReturnsNull_WhenNoPolicyMatches()
        {
            var policies = new[]
            {
            CreatePolicy(1, "/admin/*"),
            CreatePolicy(2, "/internal/health")
        };

            var result = _resolver.FindBestMatch(policies, "/api/orders");

            Assert.Null(result);
        }

        [Fact]
        public void FindBestMatch_KeepsFirstWildcard_WhenMultipleWildcardsExist()
        {
            var firstWildcard = CreatePolicy(1, "*");
            var secondWildcard = CreatePolicy(2, "*");

            var result = _resolver.FindBestMatch(
                new[] { firstWildcard, secondWildcard },
                "/anything");

            Assert.Same(firstWildcard, result);
        }

        private static Policy CreatePolicy(int id, string endpointPattern)
        {
            return new Policy
            {
                Id = id,
                UserId = 1,
                Name = $"Policy {id}",
                EndpointPattern = endpointPattern,
                Algorithm = RateLimitAlgorithm.TokenBucket,
                Limit = 10,
                WindowInSeconds = 60,
                CreatedAtUtc = DateTime.UtcNow
            };
        }
    }
}