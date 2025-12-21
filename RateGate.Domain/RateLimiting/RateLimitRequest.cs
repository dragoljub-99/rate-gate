namespace RateGate.Domain.RateLimiting
{
    public class RateLimitRequest
    {
        public string ApiKey { get; }
        public string Endpoint { get; }
        public int Cost { get; }
        public int Limit { get; }
        public int WindowInSeconds { get; }
        public int? BurstLimit { get; }

        public RateLimitRequest(
            string apiKey,
            string endpoint,
            int cost,
            int limit,
            int windowInSeconds,
            int? burstLimit = null)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new ArgumentException("API key must be provided.", nameof(apiKey));
            }

            if (string.IsNullOrWhiteSpace(endpoint))
            {
                throw new ArgumentException("Endpoint must be provided.", nameof(endpoint));
            }

            if (cost <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(cost), "Cost must be a positive integer.");
            }

            if (limit <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(limit), "Limit must be a positive integer.");
            }

            if (windowInSeconds <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(windowInSeconds), "Window must be a positive integer.");
            }

            ApiKey = apiKey;
            Endpoint = endpoint;
            Cost = cost;
            Limit = limit;
            WindowInSeconds = windowInSeconds;
            BurstLimit = burstLimit;
        }
    }
}