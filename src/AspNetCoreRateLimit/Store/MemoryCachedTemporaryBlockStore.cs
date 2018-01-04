using System;
using Microsoft.Extensions.Caching.Memory;
namespace AspNetCoreRateLimit
{
    public class MemoryCachedTemporaryBlockStore : ITemporaryBlockStore
    {
        readonly IMemoryCache memory;

        public MemoryCachedTemporaryBlockStore(IMemoryCache memory)
        {
            this.memory = memory;
        }

        public void Add(string requestKey, TimeSpan duration)
        {
            var entry = new TemporaryBlocked()
            {
                Timestamp = DateTime.UtcNow,
                Duration = duration
            };
            memory.Set(requestKey, entry, absoluteExpirationRelativeToNow: duration);
        }

        public TemporaryBlocked? Get(string requestKey)
        {
            TemporaryBlocked stored;
            if (memory.TryGetValue(requestKey, out stored))
                return stored;
            return null;
        }
    }
}
