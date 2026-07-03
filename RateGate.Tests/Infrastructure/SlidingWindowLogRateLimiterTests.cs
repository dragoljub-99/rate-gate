using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using RateGate.Domain.Entities;
using RateGate.Domain.RateLimiting;
using RateGate.Infrastructure.Data;
using RateGate.Infrastructure.RateLimiting;
using Xunit;

namespace RateGate.Tests.Infrastructure;

public class SlidingWindowLogRateLimiterTests
{
    private const int TestUserId = 10;
    private const int TestApiKeyId = 10;
    private const string TestApiKey = "test-api-key";

    [Fact]
    public async Task CheckAsync_AllowsRequest_WhenTotalCostStaysWithinWindowLimit()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        await using var dbContext = await CreateDbContextAsync(connection);

        var timeProvider = new MutableTimeProvider
        {
            UtcNow = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc)
        };

        await SeedActiveApiKeyAsync(dbContext, timeProvider.UtcNow);

        var limiter = new SlidingWindowLogRateLimiter(dbContext, timeProvider);

        var result = await limiter.CheckAsync(new RateLimitRequest(
            apiKey: TestApiKey,
            endpoint: "/api/orders",
            cost: 3,
            limit: 5,
            windowInSeconds: 10));

        Assert.True(result.IsAllowed);
        Assert.Equal(RateLimitDecisionReason.Allowed, result.Reason);
        Assert.Equal(2, result.Remaining);

        var usageLog = await dbContext.UsageLogs.SingleAsync();

        Assert.Equal(TestApiKeyId, usageLog.ApiKeyId);
        Assert.Equal("/api/orders", usageLog.Endpoint);
        Assert.Equal(3, usageLog.Cost);
        Assert.Equal(timeProvider.UtcNow, usageLog.OccurredAtUtc);
    }

    [Fact]
    public async Task CheckAsync_DeniesRequest_WhenTotalCostWouldExceedWindowLimit()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        await using var dbContext = await CreateDbContextAsync(connection);

        var now = new DateTime(2026, 1, 1, 12, 0, 10, DateTimeKind.Utc);

        var timeProvider = new MutableTimeProvider
        {
            UtcNow = now
        };

        await SeedActiveApiKeyAsync(dbContext, now);

        dbContext.UsageLogs.AddRange(
            new UsageLog
            {
                ApiKeyId = TestApiKeyId,
                Endpoint = "/api/orders",
                Cost = 3,
                OccurredAtUtc = now.AddSeconds(-8)
            },
            new UsageLog
            {
                ApiKeyId = TestApiKeyId,
                Endpoint = "/api/orders",
                Cost = 2,
                OccurredAtUtc = now.AddSeconds(-5)
            });

        await dbContext.SaveChangesAsync();

        var limiter = new SlidingWindowLogRateLimiter(dbContext, timeProvider);

        var result = await limiter.CheckAsync(new RateLimitRequest(
            apiKey: TestApiKey,
            endpoint: "/api/orders",
            cost: 1,
            limit: 5,
            windowInSeconds: 10));

        Assert.False(result.IsAllowed);
        Assert.Equal(RateLimitDecisionReason.LimitExceeded, result.Reason);
        Assert.Equal(0, result.Remaining);

        // The oldest relevant log happened 8 seconds ago.
        // With a 10-second window, it expires in 2 seconds.
        Assert.Equal(2000, result.RetryAfterMs);

        Assert.Equal(2, await dbContext.UsageLogs.CountAsync());
    }

    [Fact]
    public async Task CheckAsync_IgnoresUsageLogsOutsideTheWindow()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        await using var dbContext = await CreateDbContextAsync(connection);

        var now = new DateTime(2026, 1, 1, 12, 0, 10, DateTimeKind.Utc);

        var timeProvider = new MutableTimeProvider
        {
            UtcNow = now
        };

        await SeedActiveApiKeyAsync(dbContext, now);

        dbContext.UsageLogs.Add(new UsageLog
        {
            ApiKeyId = TestApiKeyId,
            Endpoint = "/api/orders",
            Cost = 5,
            OccurredAtUtc = now.AddSeconds(-11)
        });

        await dbContext.SaveChangesAsync();

        var limiter = new SlidingWindowLogRateLimiter(dbContext, timeProvider);

        var result = await limiter.CheckAsync(new RateLimitRequest(
            apiKey: TestApiKey,
            endpoint: "/api/orders",
            cost: 5,
            limit: 5,
            windowInSeconds: 10));

        Assert.True(result.IsAllowed);
        Assert.Equal(RateLimitDecisionReason.Allowed, result.Reason);
        Assert.Equal(0, result.Remaining);

        Assert.Equal(2, await dbContext.UsageLogs.CountAsync());
    }

    [Fact]
    public async Task CheckAsync_Denies_WhenApiKeyIsInactive()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        await using var dbContext = await CreateDbContextAsync(connection);

        var now = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

        var timeProvider = new MutableTimeProvider
        {
            UtcNow = now
        };

        await SeedActiveApiKeyAsync(dbContext, now, isActive: false);

        var limiter = new SlidingWindowLogRateLimiter(dbContext, timeProvider);

        var result = await limiter.CheckAsync(new RateLimitRequest(
            apiKey: TestApiKey,
            endpoint: "/api/orders",
            cost: 1,
            limit: 5,
            windowInSeconds: 10));

        Assert.False(result.IsAllowed);
        Assert.Equal(RateLimitDecisionReason.ApiKeyInvalidOrInactive, result.Reason);
        Assert.Empty(await dbContext.UsageLogs.ToListAsync());
    }

    private static async Task<RateGateDbContext> CreateDbContextAsync(
        SqliteConnection connection)
    {
        var options = new DbContextOptionsBuilder<RateGateDbContext>()
            .UseSqlite(connection)
            .Options;

        var dbContext = new RateGateDbContext(options);

        await dbContext.Database.EnsureCreatedAsync();

        return dbContext;
    }

    private static async Task SeedActiveApiKeyAsync(
        RateGateDbContext dbContext,
        DateTime createdAtUtc,
        bool isActive = true)
    {
        dbContext.Users.Add(new User
        {
            Id = TestUserId,
            Name = "Test Tenant",
            Email = "test@example.com",
            Plan = "test",
            CreatedAtUtc = createdAtUtc
        });

        dbContext.ApiKeys.Add(new ApiKey
        {
            Id = TestApiKeyId,
            UserId = TestUserId,
            Key = TestApiKey,
            IsActive = isActive,
            CreatedAtUtc = createdAtUtc,
            LastUsedAtUtc = null
        });

        await dbContext.SaveChangesAsync();
    }
}