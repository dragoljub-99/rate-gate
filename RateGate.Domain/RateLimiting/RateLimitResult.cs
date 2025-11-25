namespace RateGate.Domain.RateLimiting
{
    public class RateLimitResult
    {
        public bool IsAllowed { get; }
        public int? RetryAfterMs { get; }
        public int? Remaining { get; }
        public RateLimitDecisionReason Reason { get; }
        public string? Message { get; }

        public RateLimitResult(
            bool isAllowed,
            RateLimitDecisionReason reason,
            int? retryAfterMs = null,
            int? remaining = null,
            string? message = null)
        {
            IsAllowed = isAllowed;
            Reason = reason;
            RetryAfterMs = retryAfterMs;
            Remaining = remaining;
            Message = message;
        }

        public static RateLimitResult Allow(int? remaining = null, string? message = null)
            => new RateLimitResult(
                isAllowed: true,
                reason: RateLimitDecisionReason.Allowed,
                retryAfterMs: null,
                remaining: remaining,
                message: message);

        public static RateLimitResult Deny(
            RateLimitDecisionReason reason,
            int? retryAfterMs = null,
            int? remaining = null,
            string? message = null)
            => new RateLimitResult(
                isAllowed: false,
                reason: reason,
                retryAfterMs: retryAfterMs,
                remaining: remaining,
                message: message);
    }
}
