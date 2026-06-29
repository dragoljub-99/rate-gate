using RateGate.Domain.RateLimiting;
using RateGate.Infrastructure;
using RateGate.Infrastructure.Data;
using RateGate.Api.Models;
using Microsoft.EntityFrameworkCore;
using RateGate.Api.Models.Admin;
using RateGate.Domain.Entities;

namespace RateGate.Api.Services.Admin
{
    public class AdminPoliciesService
    {
        private readonly RateGateDbContext _dbContext;
        private readonly ITimeProvider _timeProvider;

        public AdminPoliciesService(RateGateDbContext dbContext, ITimeProvider timeProvider)
        {
            _dbContext = dbContext;
            _timeProvider = timeProvider;
        }

        public async Task<IEnumerable<AdminPolicyDto>> GetAllAsync(CancellationToken cancellationToken)
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

            return result;
        }

        public async Task<AdminPolicyDto?> GetByIdAsync(int id, CancellationToken cancellationToken)
        {
            var policy = await _dbContext.Policies
                                         .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

            if (policy is null)
            {
                return null;
            }

            return MappToDto(policy);
        }

        public async Task<(AdminPolicyDto? adminPolicyDto, string? errorMessage)> CreateAsync(AdminPolicyCreateDto dto, 
                                                                             CancellationToken cancellationToken)
        {
            bool userExists = await _dbContext.Users
                                              .AnyAsync(u => u.Id == dto.UserId, cancellationToken);

            if (!userExists)
            {
                return (null, $"User with id {dto.UserId} does not exist");
            }

            var policy = new Policy
            {
                UserId = dto.UserId,
                Name = dto.Name,
                EndpointPattern = dto.EndpointPattern,
                Algorithm = dto.Algorithm,
                Limit = dto.Limit,
                WindowInSeconds = dto.WindowInSeconds,
                BurstLimit = dto.BurstLimit,
                CreatedAtUtc = _timeProvider.UtcNow
            };

            _dbContext.Policies.Add(policy);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return (MappToDto(policy), null);
        }

        public async Task<AdminPolicyDto?> UpdateAsync(int id, AdminPolicyUpdateDto dto,
                                                       CancellationToken cancellationToken)
        {
            var policy = await _dbContext.Policies
                                         .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
                                         
            if (policy is null)
            {
                return null;
            }

            policy.Name = dto.Name;
            policy.EndpointPattern = dto.EndpointPattern;
            policy.Algorithm = dto.Algorithm;
            policy.Limit = dto.Limit;
            policy.WindowInSeconds = dto.WindowInSeconds;
            policy.BurstLimit = dto.BurstLimit;

            await _dbContext.SaveChangesAsync(cancellationToken);

            return MappToDto(policy);
        }

        public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken)
        {
            var policy = await _dbContext.Policies
                                         .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

            if (policy is null)
            {
                return false;
            }

            _dbContext.Policies.Remove(policy);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return true;
        }

        private static AdminPolicyDto MappToDto(Policy policy)
        {
            return new AdminPolicyDto
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
        }
    }
}