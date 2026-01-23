using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RateGate.Api.Models.Admin;
using RateGate.Infrastructure.Data;

namespace RateGate.Api.Controllers
{
    [ApiController]
    [Route("admin/metrics")]
    public class AdminMetricsController : ControllerBase
    {
        private readonly RateGateDbContext _dbContext;

        public AdminMetricsController(RateGateDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpGet("users")]
        public async Task<ActionResult<IEnumerable<AdminUserMetricsDto>>> GetUsersMetrics(
            [FromQuery] int windowSeconds = 3600,
            CancellationToken cancellationToken = default)
        {
            if (windowSeconds <= 0)
            {
                return BadRequest("windowSeconds must be a positive integer.");
            }

            var now = DateTime.UtcNow;
            var windowStart = now.AddSeconds(-windowSeconds);

            var users = await _dbContext.Users
                .Include(u => u.ApiKeys)
                .Include(u => u.Policies)
                .ToListAsync(cancellationToken);

            var usageAggregation = await (
                from log in _dbContext.UsageLogs
                join key in _dbContext.ApiKeys on log.ApiKeyId equals key.Id
                where log.OccurredAtUtc >= windowStart
                group log by key.UserId
                into g
                select new
                {
                    UserId = g.Key,
                    TotalRequests = g.Count(),
                    LastRequestAtUtc = g.Max(l => l.OccurredAtUtc)
                })
                .ToListAsync(cancellationToken);

            var usageByUserId = usageAggregation
                .ToDictionary(x => x.UserId, x => x);

            var result = users.Select(u =>
            {
                usageByUserId.TryGetValue(u.Id, out var usage);

                return new AdminUserMetricsDto
                {
                    UserId = u.Id,
                    Name = u.Name,
                    Email = u.Email,
                    ApiKeysCount = u.ApiKeys.Count,
                    PoliciesCount = u.Policies.Count,
                    TotalRequests = usage?.TotalRequests ?? 0,
                    LastRequestAtUtc = usage?.LastRequestAtUtc
                };
            });

            return Ok(result);
        }

        [HttpGet("users/{userId:int}")]
        public async Task<ActionResult<IEnumerable<AdminUserEndpointMetricsDto>>> GetUserEndpointMetrics(
            int userId,
            [FromQuery] int windowSeconds = 3600,
            CancellationToken cancellationToken = default)
        {
            if (windowSeconds <= 0)
            {
                return BadRequest("windowSeconds must be a positive integer.");
            }

            var userExists = await _dbContext.Users
                .AnyAsync(u => u.Id == userId, cancellationToken);

            if (!userExists)
            {
                return NotFound();
            }

            var now = DateTime.UtcNow;
            var windowStart = now.AddSeconds(-windowSeconds);

            var endpointMetrics = await (
                from log in _dbContext.UsageLogs
                join key in _dbContext.ApiKeys on log.ApiKeyId equals key.Id
                where key.UserId == userId && log.OccurredAtUtc >= windowStart
                group log by log.Endpoint
                into g
                select new AdminUserEndpointMetricsDto
                {
                    Endpoint = g.Key,
                    RequestCount = g.Count(),
                    LastRequestAtUtc = g.Max(l => l.OccurredAtUtc)
                })
                .OrderByDescending(m => m.RequestCount)
                .ToListAsync(cancellationToken);

            return Ok(endpointMetrics);
        }
    }
}