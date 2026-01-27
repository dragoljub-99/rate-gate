using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RateGate.Domain.RateLimiting;
using RateGate.Infrastructure.Data;
using RateGate.Infrastructure.RateLimiting;
using RateGate.Infrastructure.Time;
using System.Text.Json.Serialization;

namespace RateGate.Api
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {

            services
                .AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                });

            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen();

            services.AddCors(options =>
{
         options.AddPolicy("FrontendDev", policy =>
        {
            policy
                .WithOrigins(
               "http://127.0.0.1:5000",
               "http://localhost:5000"
           )
           .AllowAnyHeader()
           .AllowAnyMethod();
        });
     });


            var connectionString = Configuration.GetConnectionString("RateGateDatabase");

            services.AddDbContext<RateGateDbContext>(options =>
            {
                options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
            });

            services.AddSingleton<ITimeProvider, SystemTimeProvider>();

            services.AddSingleton<TokenBucketRateLimiter>(sp =>
            {
                var timeProvider = sp.GetRequiredService<ITimeProvider>();
                return new TokenBucketRateLimiter(timeProvider);
            });

            services.AddSingleton<IRateLimiter>(sp =>
                sp.GetRequiredService<TokenBucketRateLimiter>());

            services.AddScoped<SlidingWindowLogRateLimiter>(sp =>
            {
                var dbContext = sp.GetRequiredService<RateGateDbContext>();
                var timeProvider = sp.GetRequiredService<ITimeProvider>();

                return new SlidingWindowLogRateLimiter(dbContext, timeProvider);
            });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI();
            }
            app.UseStaticFiles();
            app.UseRouting();

            app.UseCors("FrontendDev");

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}