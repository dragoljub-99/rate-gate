using Microsoft.EntityFrameworkCore;
using RateGate.Domain.Entities;

namespace RateGate.Infrastructure.Data
{
    public class RateGateDbContext : DbContext
    {
        public RateGateDbContext(DbContextOptions<RateGateDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users => Set<User>();

        public DbSet<ApiKey> ApiKeys => Set<ApiKey>();

        public DbSet<Policy> Policies => Set<Policy>();

        public DbSet<UsageLog> UsageLogs => Set<UsageLog>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("users");

                entity.HasKey(u => u.Id);

                entity.Property(u => u.Name)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(u => u.Email)
                    .HasMaxLength(320);

                entity.Property(u => u.Plan)
                    .HasMaxLength(100);

                entity.Property(u => u.CreatedAtUtc)
                    .IsRequired();
            });

            modelBuilder.Entity<ApiKey>(entity =>
            {
                entity.ToTable("api_keys");

                entity.HasKey(k => k.Id);

                entity.Property(k => k.Key)
                    .IsRequired()
                    .HasMaxLength(128);

                entity.Property(k => k.IsActive)
                    .IsRequired();

                entity.Property(k => k.CreatedAtUtc)
                    .IsRequired();

                entity.Property(k => k.LastUsedAtUtc)
                    .IsRequired(false);

                entity.HasIndex(k => k.Key)
                    .IsUnique();

                entity.HasOne(k => k.User)
                    .WithMany(u => u.ApiKeys)
                    .HasForeignKey(k => k.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Policy>(entity =>
            {
                entity.ToTable("policies");

                entity.HasKey(p => p.Id);

                entity.Property(p => p.Name)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(p => p.EndpointPattern)
                    .IsRequired()
                    .HasMaxLength(400);

                entity.Property(p => p.Algorithm)
                    .IsRequired();

                entity.Property(p => p.Limit)
                    .IsRequired();

                entity.Property(p => p.WindowInSeconds)
                    .IsRequired();

                entity.Property(p => p.BurstLimit)
                    .IsRequired(false);

                entity.Property(p => p.CreatedAtUtc)
                    .IsRequired();

                entity.HasOne(p => p.User)
                    .WithMany(u => u.Policies)
                    .HasForeignKey(p => p.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<UsageLog>(entity =>
            {
                entity.ToTable("usage_logs");

                entity.HasKey(x => x.Id);

                entity.Property(x => x.Endpoint)
                    .IsRequired()
                    .HasMaxLength(400);

                entity.Property(x => x.OccurredAtUtc)
                    .IsRequired();

                entity.Property(x => x.Cost)
                    .IsRequired();

                entity.HasOne(x => x.ApiKey)
                    .WithMany()
                    .HasForeignKey(x => x.ApiKeyId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(x => new { x.ApiKeyId, x.Endpoint, x.OccurredAtUtc });
            });


            var demoUserId = 1;
            var demoApiKeyId = 1;
            var tokenBucketPolicyId = 1;
            var slidingPolicyId = 2;

            modelBuilder.Entity<User>().HasData(new User
            {
                Id = demoUserId,
                Name = "Demo Tenant",
                Email = "demo@example.com",
                Plan = "free",
                CreatedAtUtc = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            });

            modelBuilder.Entity<ApiKey>().HasData(new ApiKey
            {
                Id = demoApiKeyId,
                UserId = demoUserId,
                Key = "demo-key-1",
                IsActive = true,
                CreatedAtUtc = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                LastUsedAtUtc = null
            });

            modelBuilder.Entity<Policy>().HasData(new Policy
            {
                Id = tokenBucketPolicyId,
                UserId = demoUserId,
                Name = "Default demo policy (token bucket)",
                EndpointPattern = "*",
                Algorithm = RateLimitAlgorithm.TokenBucket,
                Limit = 10,
                WindowInSeconds = 10,
                BurstLimit = 5,
                CreatedAtUtc = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            });

            modelBuilder.Entity<Policy>().HasData(new Policy
            {
                Id = slidingPolicyId,
                UserId = demoUserId,
                Name = "Sliding window demo policy",
                EndpointPattern = "/sliding-demo",
                Algorithm = RateLimitAlgorithm.SlidingWindowLog,
                Limit = 10,
                WindowInSeconds = 10,
                BurstLimit = null,
                CreatedAtUtc = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            });
        }
    }
}
