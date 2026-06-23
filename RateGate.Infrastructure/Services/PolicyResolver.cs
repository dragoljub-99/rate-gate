using RateGate.Domain.Entities;
using RateGate.Domain.Abstractions;

namespace RateGate.Infrastructure.Services
{
    public class PolicyResolver : IPolicyResolver
    {
        public Policy? FindBestMatch(IEnumerable<Policy> policies, string endpoint)
        {
             Policy? wildCardMatch = null;
             Policy? prefixMatch = null;
             Policy? exactMatch = null;

             foreach (var policy in policies)
            {
                var pattern = policy.EndpointPattern;

                if (pattern == "*")
                {
                    wildCardMatch ??= policy;
                    continue;
                }

                if (pattern.EndsWith("/*", StringComparison.Ordinal))
                {
                    var prefix = pattern.Substring(0, pattern.Length - 1);
                    if (endpoint.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    {
                        prefixMatch ??= policy;
                    }

                    continue;
                }

                if (string.Equals(pattern, endpoint, StringComparison.OrdinalIgnoreCase))
                {
                    exactMatch ??= policy;                   
                }
            }

            return exactMatch ?? prefixMatch ?? wildCardMatch;
        }
       

        
    }
}