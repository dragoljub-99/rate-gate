using RateGate.Domain.Entities;
using RateGate.Domain.Abstractions;

namespace RateGate.Infrastructure.Services
{
    public class PolicyResolver : IPolicyResolver
    {
        public Policy? FindBestMatch(IEnumerable<Policy> policies, string endpoint)
        {
             Policy? wildCardMatch = null;
             Policy? bestPrefixMatch = null;
             var longestPrefixLength = 0;

             foreach (var policy in policies)
            {
                var pattern = policy.EndpointPattern;

                if (string.Equals(pattern, endpoint, StringComparison.OrdinalIgnoreCase))
                {
                    return policy;
                }

                if (pattern.EndsWith("*/", StringComparison.Ordinal))
                {
                    var prefix = pattern.Substring(0, pattern.Length - 1);

                    if (endpoint.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) &&
                                            prefix.Length > longestPrefixLength)
                    {
                        bestPrefixMatch = policy;
                        longestPrefixLength = prefix.Length;
                    }

                    continue;
                }

                if (endpoint == "*")
                {
                    wildCardMatch ??= policy;
                }
            }

            return bestPrefixMatch ?? wildCardMatch;
                
        }
       

        
    }
}