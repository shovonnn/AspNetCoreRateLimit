using System;
using System.Threading.Tasks;

namespace AspNetCoreRateLimit
{
    public class RateLimitCounterStoreAsync : IRateLimitCounterStoreAsync
    {
        readonly ICacheClient cache;

        public RateLimitCounterStoreAsync(ICacheClient cache)
        {
            this.cache = cache;
        }

        public async Task<RateLimitCounter> CreateCounter(string id, TimeSpan expirationTime)
        {
            var counter = new RateLimitCounter()
            {
                Timestamp = DateTime.UtcNow,
                TotalRequests = 0
            };
            if (!await cache.AtomicAdd(new AtomicAddOperation(_keyRequestCount(id), counter.TotalRequests.ToString(), expirationTime),
                                      new AtomicAddOperation(_keyTimestamp(id), counter.Timestamp.ToString("O"), expirationTime)))
            {
                throw new ConcurrencyException();
            }
            return counter;
        }

        public async Task<RateLimitCounter?> GetAsync(string id)
        {
            var timestamp = await cache.Get(_keyTimestamp(id));
            var total_requ = await cache.Get(_keyRequestCount(id));
            if (timestamp == null || total_requ == null) return null;
            return new RateLimitCounter()
            {
                Timestamp = DateTime.Parse(timestamp).ToUniversalTime(),
                TotalRequests = int.Parse(total_requ)
            };
        }

        public async Task<RateLimitCounter> IncrementAsync(string id, RateLimitCounter counterObj)
        {
            await cache.Increament(_keyRequestCount(id));
            return new RateLimitCounter()
            {
                Timestamp = counterObj.Timestamp,
                TotalRequests = counterObj.TotalRequests + 1
            };
        }

        public async Task<RateLimitCounter> ResetCounter(string id, RateLimitCounter currentState, TimeSpan expirationTime)
        {
            var newState = new RateLimitCounter()
            {
                Timestamp = DateTime.UtcNow,
                TotalRequests = 0
            };
            if (!await cache.AtomicReplace(new AtomicReplaceOperation(_keyRequestCount(id), newState.TotalRequests.ToString(), currentState.TotalRequests.ToString(), expirationTime),
                                           new AtomicReplaceOperation(_keyTimestamp(id), newState.Timestamp.ToString("O"), currentState.Timestamp.ToString("O"), expirationTime)))
            {
                throw new ConcurrencyException();
            }
            return newState;
        }

        static string _keyRequestCount(string key) => $"{key}_request_count";
        static string _keyTimestamp(string key) => $"{key}_timestamp";
    }
}
