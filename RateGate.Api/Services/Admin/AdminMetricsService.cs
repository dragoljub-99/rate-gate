using Microsoft.EntityFrameworkCore;
using RateGate.Api.Models.Admin;
using RateGate.Domain.RateLimiting;
using RateGate.Infrastructure.Data;

namespace RateGate.Api.Services.Admin
{
    public class AdminMetricsService
    {
        private readonly RateGateDbContext _dbContext;
        private readonly ITimeProvider _timeProvider;

        public AdminMetricsService(
            RateGateDbContext dbContext,
            ITimeProvider timeProvider)
        {
            _dbContext = dbContext;
            _timeProvider = timeProvider;
        }

        public async Task<IEnumerable<AdminUserMetricsDto>> GetUsersMetricsAsync(
            int windowSeconds,
            CancellationToken cancellationToken)
        {
            var windowStart = _timeProvider.UtcNow.AddSeconds(-windowSeconds);

            var users = await _dbContext.Users
                .AsNoTracking()
                .Select(user => new
                {
                    user.Id,
                    user.Name,
                    user.Email,
                    ApiKeysCount = user.ApiKeys.Count,
                    PoliciesCount = user.Policies.Count
                })
                .ToListAsync(cancellationToken);

            var usageAggregation = await (
                from log in _dbContext.UsageLogs
                join apiKey in _dbContext.ApiKeys
                    on log.ApiKeyId equals apiKey.Id
                where log.OccurredAtUtc >= windowStart
                group log by apiKey.UserId
                into usageGroup
                select new
                {
                    UserId = usageGroup.Key,
                    TotalRequests = usageGroup.Count(),
                    LastRequestAtUtc =
                        usageGroup.Max(log => log.OccurredAtUtc)
                })
                .ToListAsync(cancellationToken);

            var usageByUserId = usageAggregation
                .ToDictionary(usage => usage.UserId);

            var result = users.Select(user =>
            {
                usageByUserId.TryGetValue(user.Id, out var usage);

                return new AdminUserMetricsDto
                {
                    UserId = user.Id,
                    Name = user.Name,
                    Email = user.Email,
                    ApiKeysCount = user.ApiKeysCount,
                    PoliciesCount = user.PoliciesCount,
                    TotalRequests = usage?.TotalRequests ?? 0,
                    LastRequestAtUtc = usage?.LastRequestAtUtc
                };
            });

            return result;
        }

        public async Task<IEnumerable<AdminUserEndpointMetricsDto>?>
            GetUserEndpointMetricsAsync(
                int userId,
                int windowSeconds,
                CancellationToken cancellationToken)
        {
            var userExists = await _dbContext.Users
                .AnyAsync(
                    user => user.Id == userId,
                    cancellationToken);

            if (!userExists)
            {
                return null;
            }

            var windowStart = _timeProvider.UtcNow.AddSeconds(-windowSeconds);

            var endpointMetrics = await (
                from log in _dbContext.UsageLogs
                join apiKey in _dbContext.ApiKeys
                    on log.ApiKeyId equals apiKey.Id
                where apiKey.UserId == userId &&
                      log.OccurredAtUtc >= windowStart
                group log by log.Endpoint
                into endpointGroup
                select new AdminUserEndpointMetricsDto
                {
                    Endpoint = endpointGroup.Key,
                    RequestCount = endpointGroup.Count(),
                    LastRequestAtUtc =
                        endpointGroup.Max(log => log.OccurredAtUtc)
                })
                .OrderByDescending(metric => metric.RequestCount)
                .ToListAsync(cancellationToken);

            return endpointMetrics;
        }
    }
}