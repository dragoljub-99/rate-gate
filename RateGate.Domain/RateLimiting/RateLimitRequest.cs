namespace RateGate.Domain.RateLimiting
{
    public class RateLimitRequest
    {
        public string ApiKey { get; }
        public string Endpoint { get; }
        public int Cost { get; }

        public RateLimitRequest(string apiKey, string endpoint, int cost = 1)
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

            ApiKey = apiKey;
            Endpoint = endpoint;
            Cost = cost;
        }
    }
}