using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RateGate.Api.Models.Admin;
using RateGate.Infrastructure.Data;

namespace RateGate.Api.Controllers
{
    [ApiController]
    [Route("admin/apikeys")]
    public class AdminApiKeysController : ControllerBase
    {
        private readonly RateGateDbContext _dbContext;

        public AdminApiKeysController(RateGateDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpPost]
        public async Task<ActionResult<AdminApiKeyDto>> Create(
            [FromBody] AdminApiKeyCreateDto dto,
            CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = await _dbContext.Users
                .FirstOrDefaultAsync(u => u.Id == dto.UserId, cancellationToken);

            if (user == null)
            {
                return BadRequest($"User with id {dto.UserId} does not exist.");
            }

            var key = string.IsNullOrWhiteSpace(dto.Key)
                ? GenerateApiKey()
                : dto.Key.Trim();

            var isActive = dto.IsActive ?? true;
            var now = DateTime.UtcNow;

            var apiKey = new Domain.Entities.ApiKey
            {
                UserId = dto.UserId,
                Key = key,
                IsActive = isActive,
                CreatedAtUtc = now,
                LastUsedAtUtc = null
            };

            _dbContext.ApiKeys.Add(apiKey);
            await _dbContext.SaveChangesAsync(cancellationToken);

            var result = new AdminApiKeyDto
            {
                Id = apiKey.Id,
                Key = apiKey.Key,
                IsActive = apiKey.IsActive,
                CreatedAtUtc = apiKey.CreatedAtUtc,
                LastUsedAtUtc = apiKey.LastUsedAtUtc,
                UserId = apiKey.UserId
            };

            return CreatedAtAction(
                nameof(GetById),
                new { id = apiKey.Id },
                result);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<AdminApiKeyDto>> GetById(int id, CancellationToken cancellationToken)
        {
            var apiKey = await _dbContext.ApiKeys
                .FirstOrDefaultAsync(k => k.Id == id, cancellationToken);

            if (apiKey == null)
            {
                return NotFound();
            }

            var result = new AdminApiKeyDto
            {
                Id = apiKey.Id,
                Key = apiKey.Key,
                IsActive = apiKey.IsActive,
                CreatedAtUtc = apiKey.CreatedAtUtc,
                LastUsedAtUtc = apiKey.LastUsedAtUtc,
                UserId = apiKey.UserId
            };

            return Ok(result);
        }

        [HttpPost("{id:int}/activate")]
        public async Task<ActionResult<AdminApiKeyDto>> Activate(int id, CancellationToken cancellationToken)
        {
            var apiKey = await _dbContext.ApiKeys
                .FirstOrDefaultAsync(k => k.Id == id, cancellationToken);

            if (apiKey == null)
            {
                return NotFound();
            }

            apiKey.IsActive = true;
            await _dbContext.SaveChangesAsync(cancellationToken);

            var result = new AdminApiKeyDto
            {
                Id = apiKey.Id,
                Key = apiKey.Key,
                IsActive = apiKey.IsActive,
                CreatedAtUtc = apiKey.CreatedAtUtc,
                LastUsedAtUtc = apiKey.LastUsedAtUtc,
                UserId = apiKey.UserId
            };

            return Ok(result);
        }

        [HttpPost("{id:int}/deactivate")]
        public async Task<ActionResult<AdminApiKeyDto>> Deactivate(int id, CancellationToken cancellationToken)
        {
            var apiKey = await _dbContext.ApiKeys
                .FirstOrDefaultAsync(k => k.Id == id, cancellationToken);

            if (apiKey == null)
            {
                return NotFound();
            }

            apiKey.IsActive = false;
            await _dbContext.SaveChangesAsync(cancellationToken);

            var result = new AdminApiKeyDto
            {
                Id = apiKey.Id,
                Key = apiKey.Key,
                IsActive = apiKey.IsActive,
                CreatedAtUtc = apiKey.CreatedAtUtc,
                LastUsedAtUtc = apiKey.LastUsedAtUtc,
                UserId = apiKey.UserId
            };

            return Ok(result);
        }

        private static string GenerateApiKey()
        {
            var bytes = RandomNumberGenerator.GetBytes(32);
            var sb = new StringBuilder(bytes.Length * 2);
            foreach (var b in bytes)
            {
                sb.Append(b.ToString("x2"));
            }
            return sb.ToString();
        }
    }
}
