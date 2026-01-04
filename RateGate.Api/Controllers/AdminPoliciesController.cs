using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RateGate.Api.Models.Admin;
using RateGate.Domain.Entities;
using RateGate.Infrastructure.Data;

namespace RateGate.Api.Controllers
{
    [ApiController]
    [Route("admin/policies")]
    public class AdminPoliciesController : ControllerBase
    {
        private readonly RateGateDbContext _dbContext;

        public AdminPoliciesController(RateGateDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<AdminPolicyDto>>> GetAll(CancellationToken cancellationToken)
        {
            var policies = await _dbContext.Policies
                .AsNoTracking()
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

        [HttpGet("{id:int}")]
        public async Task<ActionResult<AdminPolicyDto>> GetById(int id, CancellationToken cancellationToken)
        {
            var policy = await _dbContext.Policies
                .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

            if (policy == null)
            {
                return NotFound();
            }

            var result = new AdminPolicyDto
            {
                Id = policy.Id,
                UserId = policy.UserId,
                Name = policy.Name,
                EndpointPattern = policy.EndpointPattern,
                Algorithm = policy.Algorithm,
                Limit = policy.Limit,
                WindowInSeconds = policy.WindowInSeconds,
                BurstLimit = policy.BurstLimit,
                CreatedAtUtc = policy.CreatedAtUtc
            };

            return Ok(result);
        }

        [HttpPost]
        public async Task<ActionResult<AdminPolicyDto>> Create(
            [FromBody] AdminPolicyCreateDto dto,
            CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userExists = await _dbContext.Users
                .AnyAsync(u => u.Id == dto.UserId, cancellationToken);

            if (!userExists)
            {
                return BadRequest($"User with id {dto.UserId} does not exist.");
            }

            var now = DateTime.UtcNow;

            var policy = new Policy
            {
                UserId = dto.UserId,
                Name = dto.Name,
                EndpointPattern = dto.EndpointPattern,
                Algorithm = dto.Algorithm,
                Limit = dto.Limit,
                WindowInSeconds = dto.WindowInSeconds,
                BurstLimit = dto.BurstLimit,
                CreatedAtUtc = now
            };

            _dbContext.Policies.Add(policy);
            await _dbContext.SaveChangesAsync(cancellationToken);

            var result = new AdminPolicyDto
            {
                Id = policy.Id,
                UserId = policy.UserId,
                Name = policy.Name,
                EndpointPattern = policy.EndpointPattern,
                Algorithm = policy.Algorithm,
                Limit = policy.Limit,
                WindowInSeconds = policy.WindowInSeconds,
                BurstLimit = policy.BurstLimit,
                CreatedAtUtc = policy.CreatedAtUtc
            };

            return CreatedAtAction(nameof(GetById), new { id = policy.Id }, result);
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult<AdminPolicyDto>> Update(
            int id,
            [FromBody] AdminPolicyUpdateDto dto,
            CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var policy = await _dbContext.Policies
                .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

            if (policy == null)
            {
                return NotFound();
            }

            policy.Name = dto.Name;
            policy.EndpointPattern = dto.EndpointPattern;
            policy.Algorithm = dto.Algorithm;
            policy.Limit = dto.Limit;
            policy.WindowInSeconds = dto.WindowInSeconds;
            policy.BurstLimit = dto.BurstLimit;

            await _dbContext.SaveChangesAsync(cancellationToken);

            var result = new AdminPolicyDto
            {
                Id = policy.Id,
                UserId = policy.UserId,
                Name = policy.Name,
                EndpointPattern = policy.EndpointPattern,
                Algorithm = policy.Algorithm,
                Limit = policy.Limit,
                WindowInSeconds = policy.WindowInSeconds,
                BurstLimit = policy.BurstLimit,
                CreatedAtUtc = policy.CreatedAtUtc
            };

            return Ok(result);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
        {
            var policy = await _dbContext.Policies
                .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

            if (policy == null)
            {
                return NotFound();
            }

            _dbContext.Policies.Remove(policy);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return NoContent();
        }
    }
}
