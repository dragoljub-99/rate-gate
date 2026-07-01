using RateGate.Domain.RateLimiting;
using RateGate.Infrastructure.Data;
using RateGate.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using RateGate.Api.Models.Admin;

namespace RateGate.Api.Services.Admin
{
    public class AdminUsersService
    {
        private readonly RateGateDbContext _dbContext;
        private readonly ITimeProvider _timeProvider;

        public AdminUsersService(RateGateDbContext dbContext, ITimeProvider timeProvider)
        {
            _dbContext = dbContext;
            _timeProvider = timeProvider;
        }

        public async Task<IEnumerable<AdminUserDto>> GetAllAsync(CancellationToken cancellationToken)
        {
            return await _dbContext.Users
                         .AsNoTracking()
                         .Select(u => new AdminUserDto
                         {
                             Id = u.Id,
                             Name = u.Name,
                             Email = u.Email,
                             Plan = u.Plan,
                             CreatedAtUtc = u.CreatedAtUtc,
                             ApiKeysCount = u.ApiKeys.Count,
                             PoliciesCount = u.Policies.Count
                         })
                         .ToListAsync(cancellationToken);
        }

        public async Task<AdminUserDetailsDto?> GetByIdAsync(int id, CancellationToken cancellationToken)
        {
            return await _dbContext.Users
                .AsNoTracking()
                .AsSplitQuery()
                .Where(u => u.Id == id)
                .Select(u => new AdminUserDetailsDto
                {
                    Id = u.Id,
                    Name = u.Name,
                    Email = u.Email,
                    Plan = u.Plan,
                    CreatedAtUtc = u.CreatedAtUtc,

                    ApiKeys = u.ApiKeys
                        .Select(k => new AdminApiKeyDto
                        {
                            Id = k.Id,
                            Key = k.Key,
                            IsActive = k.IsActive,
                            CreatedAtUtc = k.CreatedAtUtc,
                            LastUsedAtUtc = k.LastUsedAtUtc,
                            UserId = k.UserId
                        })
                        .ToList(),

                    Policies = u.Policies
                        .Select(p => new AdminPolicyDto
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
                        .ToList()
                })
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<AdminUserDto> CreateAsync(AdminUserCreateDto dto, CancellationToken cancellationToken)
        {
            var user = new User
            {
                Name = dto.Name,
                Email = dto.Email,
                Plan = dto.Plan,
                CreatedAtUtc = _timeProvider.UtcNow
            };

            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync(cancellationToken);

            var result = new AdminUserDto
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                Plan = user.Plan,
                CreatedAtUtc = user.CreatedAtUtc,
                ApiKeysCount = 0,
                PoliciesCount = 0
            };

            return result;
        }

        public async Task<IEnumerable<AdminApiKeyDto>?> GetApiKeysForUserAsync(int id, CancellationToken cancellationToken)
        {
            var userExists = await _dbContext.Users
                                             .AnyAsync(u => u.Id == id, cancellationToken);

            if (!userExists)
            {
                return null;
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

            return result;
        }

        public async Task<IEnumerable<AdminPolicyDto>?> GetPoliciesForUserAsync(int id, CancellationToken cancellationToken)
        {
            var userExists = await _dbContext.Users
                                             .AnyAsync(u => u.Id == id, cancellationToken);

            if (!userExists)
            {
                return null;
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

            return result;                       
        }
    }
}
