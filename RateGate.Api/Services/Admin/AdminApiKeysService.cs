using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using RateGate.Api.Models.Admin;
using RateGate.Infrastructure.Data;
using System.Text;
using RateGate.Domain.RateLimiting;
using RateGate.Domain.Entities;

namespace RateGate.Api.Services.Admin
{
    public class AdminApiKeysService
    {
        private readonly RateGateDbContext _dbContext;
        private readonly ITimeProvider _timeProvider;

        public AdminApiKeysService(RateGateDbContext dbContext, ITimeProvider timeProvider)
        {
            _dbContext = dbContext;
            _timeProvider = timeProvider;
        }

        public async Task<(AdminApiKeyDto? apiKey, string? errorMessage)> CreateAsync(AdminApiKeyCreateDto dto, CancellationToken cancellationToken)
        {
            var user = await _dbContext.Users
                                       .FirstOrDefaultAsync(k => dto.UserId == k.Id, cancellationToken);

            if (user == null)
            {
                return (null, $"User with id {dto.UserId} does not exist");
            }

            var key = string.IsNullOrWhiteSpace(dto.Key)
                      ? GenerateApiKey()
                      : dto.Key.Trim();

            var isActive = dto.IsActive ?? true;
            var now = _timeProvider.UtcNow;

            var apiKey = new ApiKey
            {
                UserId = user.Id,
                IsActive = isActive,
                Key = key,
                CreatedAtUtc = now,
                LastUsedAtUtc = null
            };

            _dbContext.ApiKeys.Add(apiKey);
            await _dbContext.SaveChangesAsync(cancellationToken);

           return (MappToDto(apiKey), null);
        }

        public async Task<AdminApiKeyDto?> GetByIdAsync(int id, CancellationToken cancellationToken)
        {
            var key = await _dbContext.ApiKeys.FirstOrDefaultAsync(k => k.Id == id, cancellationToken);

            if (key is null)
            {
                return null;
            }

            return MappToDto(key);
        }

        public async Task<AdminApiKeyDto?> ActivateAsync(int id, CancellationToken cancellationToken)
        {
            var apiKey = await _dbContext.ApiKeys
                                        .FirstOrDefaultAsync(k => k.Id == id, cancellationToken);

            if (apiKey is null)
            {
                return null;
            }

            apiKey.IsActive = true;
            await _dbContext.SaveChangesAsync(cancellationToken);

            return MappToDto(apiKey);
        }

        public async Task<AdminApiKeyDto?> DeactivateAsync(int id, CancellationToken cancellationToken)
        {
            var apiKey = await _dbContext.ApiKeys
                                         . FirstOrDefaultAsync(k => k.Id == id, cancellationToken);

            if (apiKey is null)
            {
                return null;
            }

            apiKey.IsActive = false;
            await _dbContext.SaveChangesAsync(cancellationToken);
            
            return MappToDto(apiKey);
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

        private static AdminApiKeyDto MappToDto(ApiKey apiKey)
        {
            return new AdminApiKeyDto
            {
                Id = apiKey.Id,
                IsActive = apiKey.IsActive,
                Key = apiKey.Key,
                CreatedAtUtc = apiKey.CreatedAtUtc,
                LastUsedAtUtc = apiKey.LastUsedAtUtc,
                UserId = apiKey.UserId
            };
        }
    }


}