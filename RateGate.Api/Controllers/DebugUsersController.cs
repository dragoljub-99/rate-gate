using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RateGate.Infrastructure.Data;

namespace RateGate.Api.Controllers
{
    [ApiController]
    [Route("debug/users")]
    public class DebugUsersController : ControllerBase
    {
        private readonly RateGateDbContext _dbContext;

        public DebugUsersController(RateGateDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var users = await _dbContext.Users
                .Include(u => u.ApiKeys)
                .Include(u => u.Policies)
                .ToListAsync();

            var result = users.Select(u => new
            {
                u.Id,
                u.Name,
                u.Email,
                u.Plan,
                u.CreatedAtUtc,
                ApiKeys = u.ApiKeys.Select(k => new
                {
                    k.Id,
                    k.Key,
                    k.IsActive,
                    k.CreatedAtUtc,
                    k.LastUsedAtUtc
                }),
                Policies = u.Policies.Select(p => new
                {
                    p.Id,
                    p.Name,
                    p.EndpointPattern,
                    Algorithm = p.Algorithm.ToString(),
                    p.Limit,
                    p.WindowInSeconds,
                    p.BurstLimit,
                    p.CreatedAtUtc
                })
            });

            return Ok(result);
        }
    }
}