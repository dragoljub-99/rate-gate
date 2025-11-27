using RateGate.Domain.RateLimiting;
using RateGate.Infrastructure.Time;

namespace RateGate.ConsoleDemo
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            Console.WriteLine("RateGate Token Bucket demo");
            Console.WriteLine("Configuration: 10 requests / 10 seconds per (apiKey, endpoint)");
            Console.WriteLine();

            ITimeProvider timeProvider = new SystemTimeProvider();
            IRateLimiter limiter = new TokenBucketRateLimiter(
                capacity: 10,
                windowInSeconds: 10,
                timeProvider: timeProvider);

            var apiKey = "demo-key-1";
            var endpoint = "/demo";

            Console.WriteLine("Sending 15 quick requests (200 ms apart)...");
            Console.WriteLine();

            for (int i = 1; i <= 15; i++)
            {
                var request = new RateLimitRequest(apiKey, endpoint);
                var result = await limiter.CheckAsync(request);

                Console.WriteLine(
                    $"Request {i:00}: " +
                    $"Allowed={result.IsAllowed}, " +
                    $"Reason={result.Reason}, " +
                    $"Remaining={result.Remaining}, " +
                    $"RetryAfterMs={result.RetryAfterMs}");

                await Task.Delay(200);
            }

            Console.WriteLine();
            Console.WriteLine("Now waiting 11 seconds to let the bucket refill...");
            Console.WriteLine();

            await Task.Delay(TimeSpan.FromSeconds(11));

            for (int i = 16; i <= 20; i++)
            {
                var request = new RateLimitRequest(apiKey, endpoint);
                var result = await limiter.CheckAsync(request);

                Console.WriteLine(
                    $"Request {i:00}: " +
                    $"Allowed={result.IsAllowed}, " +
                    $"Reason={result.Reason}, " +
                    $"Remaining={result.Remaining}, " +
                    $"RetryAfterMs={result.RetryAfterMs}");

                await Task.Delay(200);
            }

            Console.WriteLine();
            Console.WriteLine("Demo complete. Press any key to exit.");
            Console.ReadKey();
        }
    }
}