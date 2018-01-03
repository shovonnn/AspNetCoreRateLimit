using System;
using System.Threading.Tasks;

namespace AspNetCoreRateLimit
{
    public interface IRateLimitCounterStoreAsync
    {
        Task<RateLimitCounter?> GetAsync(string id);
        Task<RateLimitCounter> CreateCounter(string id, TimeSpan expirationTime);
        Task<RateLimitCounter> ResetCounter(string id, RateLimitCounter currentState, TimeSpan expirationTime);
        Task<RateLimitCounter> IncrementAsync(string id, RateLimitCounter counterObj);
    }
}