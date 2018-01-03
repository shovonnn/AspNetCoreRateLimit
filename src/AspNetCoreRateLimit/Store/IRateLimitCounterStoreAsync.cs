using System;
using System.Threading.Tasks;

namespace AspNetCoreRateLimit
{
    public interface IRateLimitCounterStoreAsync
    {
        Task<bool> ExistsAsync(string id);
        Task<RateLimitCounter?> GetAsync(string id);
        Task RemoveAsync(string id);
        Task<RateLimitCounter> CreateCounter(string id, TimeSpan expirationTime);
        Task<RateLimitCounter> ResetCounter(string id, TimeSpan expirationTime);
        Task<RateLimitCounter> IncrementAsync(string id, DateTime timestamp);
    }
}