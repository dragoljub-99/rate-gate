using RateGate.Domain.RateLimiting;

namespace RateGate.Api.Models
{
    public class RateLimitCheckResponseDto
    {
        public bool Allow { get; set; }

        public string Reason { get; set; } = null!;

        public int? RetryAfterMs { get; set; }

        public int? Remaining { get; set; }

        public string? Message { get; set; }

        public static RateLimitCheckResponseDto FromDomain(RateLimitResult result)
        {
            return new RateLimitCheckResponseDto
            {
                Allow = result.IsAllowed,
                Reason = result.Reason.ToString(),
                RetryAfterMs = result.RetryAfterMs,
                Remaining = result.Remaining,
                Message = result.Message
            };
        }
    }
}
