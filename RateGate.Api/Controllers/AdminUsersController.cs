using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RateGate.Api.Models.Admin;
using RateGate.Infrastructure.Data;

namespace RateGate.Api.Controllers
{
    [ApiController]
    [Route("admin/users")]
    public class AdminUsersController : ControllerBase
    {
        private readonly RateGateDbContext _dbContext;

        public AdminUsersController(RateGateDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<AdminUserDto>>> GetAll(CancellationToken cancellationToken)
        {
            var users = await _dbContext.Users
                .Include(u => u.ApiKeys)
                .Include(u => u.Policies)
                .ToListAsync(cancellationToken);

            var result = users.Select(u => new AdminUserDto
            {
                Id = u.Id,
                Name = u.Name,
                Email = u.Email,
                Plan = u.Plan,
                CreatedAtUtc = u.CreatedAtUtc,
                ApiKeysCount = u.ApiKeys.Count,
                PoliciesCount = u.Policies.Count
            });

            return Ok(result);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<object>> GetById(int id, CancellationToken cancellationToken)
        {
            var user = await _dbContext.Users
                .Include(u => u.ApiKeys)
                .Include(u => u.Policies)
                .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

            if (user == null)
            {
                return NotFound();
            }

            var dto = new
            {
                user.Id,
                user.Name,
                user.Email,
                user.Plan,
                user.CreatedAtUtc,
                ApiKeys = user.ApiKeys.Select(k => new AdminApiKeyDto
                {
                    Id = k.Id,
                    Key = k.Key,
                    IsActive = k.IsActive,
                    CreatedAtUtc = k.CreatedAtUtc,
                    LastUsedAtUtc = k.LastUsedAtUtc,
                    UserId = k.UserId
                }),
                Policies = user.Policies.Select(p => new AdminPolicyDto
                {
                    Id = p.Id,
                    UserId = p.UserId,
                    Name = p.Name,
                    EndpointPattern = p.EndpointPattern,
                    Algorithm = p.Algorithm,
                    Limit = p.Limit,
                    WindowInSeconds = p.WindowInSeconds,
                    BurstLimit = p.BurstLimit,
                    CreatedAtUtc = p.CreatedAtUtc
                })
            };

            return Ok(dto);
        }

        [HttpGet("{id:int}/apikeys")]
        public async Task<ActionResult<IEnumerable<AdminApiKeyDto>>> GetApiKeysForUser(int id, CancellationToken cancellationToken)
        {
            var userExists = await _dbContext.Users
                .AnyAsync(u => u.Id == id, cancellationToken);

            if (!userExists)
            {
                return NotFound();
            }

            var apiKeys = await _dbContext.ApiKeys
                .Where(k => k.UserId == id)
                .ToListAsync(cancellationToken);

            var result = apiKeys.Select(k => new AdminApiKeyDto
            {
                Id = k.Id,
                Key = k.Key,
                IsActive = k.IsActive,
                CreatedAtUtc = k.CreatedAtUtc,
                LastUsedAtUtc = k.LastUsedAtUtc,
                UserId = k.UserId
            });

            return Ok(result);
        }

        [HttpGet("{id:int}/policies")]
        public async Task<ActionResult<IEnumerable<AdminPolicyDto>>> GetPoliciesForUser(int id, CancellationToken cancellationToken)
        {
            var userExists = await _dbContext.Users
                .AnyAsync(u => u.Id == id, cancellationToken);

            if (!userExists)
            {
                return NotFound();
            }

            var policies = await _dbContext.Policies
                .Where(p => p.UserId == id)
                .ToListAsync(cancellationToken);

            var result = policies.Select(p => new AdminPolicyDto
            {
                Id = p.Id,
                UserId = p.UserId,
                Name = p.Name,
                EndpointPattern = p.EndpointPattern,
                Algorithm = p.Algorithm,
                Limit = p.Limit,
                WindowInSeconds = p.WindowInSeconds,
                BurstLimit = p.BurstLimit,
                CreatedAtUtc = p.CreatedAtUtc
            });

            return Ok(result);
        }
    }
}