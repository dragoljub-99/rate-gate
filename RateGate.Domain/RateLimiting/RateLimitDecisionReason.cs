namespace RateGate.Domain.RateLimiting
{
    public enum RateLimitDecisionReason
    {
        Allowed = 0,
        ApiKeyInvalidOrInactive = 1,
        NoMatchingPolicy = 2,
        LimitExceeded = 3,
        InternalError = 4
    }
}