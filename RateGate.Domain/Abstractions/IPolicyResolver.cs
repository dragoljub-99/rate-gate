using RateGate.Domain.Entities;

namespace RateGate.Domain.Abstractions
{
    public interface IPolicyResolver
    {
         Policy? FindBestMatch(IEnumerable<Policy> policies, string endpoint);
    }
}