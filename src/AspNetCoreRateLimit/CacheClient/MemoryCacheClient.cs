using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using System.Collections.Generic;

namespace AspNetCoreRateLimit
{
    public struct MemoryCacheItem
    {
        public string Value { get; set; }
        public DateTime AbsoluteExpireDate { get; set; }
        public MemoryCacheItem(string val, TimeSpan expiresIn)
        {
            Value = val;
            AbsoluteExpireDate = DateTime.UtcNow.Add(expiresIn);
        }
    }
    public class MemoryCacheClient : ICacheClient
    {
        readonly IMemoryCache memoryCache;

        public MemoryCacheClient(IMemoryCache memoryCache)
        {
            this.memoryCache = memoryCache;
        }
        private static object _processLocker = new object();
        public Task<bool> AtomicAdd(params AtomicAddOperation[] operations)
        {
            var success = new List<AtomicAddOperation>();

            lock (_processLocker)
            {
                foreach (var opt in operations)
                {
                    if (!memoryCache.TryGetValue(opt.Key, out var val))
                    {
                        memoryCache.Set(opt.Key, new MemoryCacheItem(opt.Value, opt.ExpiresIn), opt.ExpiresIn);
                        success.Add(opt);
                    }
                    else
                    {
                        foreach (var o in success)
                        {
                            memoryCache.Remove(o.Key);
                        }
                        break;
                    }
                }
            }

            return Task.FromResult(success.Count == operations.Length);
        }

        public Task<bool> AtomicReplace(params AtomicReplaceOperation[] operations)
        {
            var success = new List<AtomicReplaceOperation>();

            lock (_processLocker)
            {
                foreach (var opt in operations)
                {
                    if (memoryCache.TryGetValue(opt.Key, out var val) && ((MemoryCacheItem)val).Value == opt.OldValue)
                    {
                        memoryCache.Set(opt.Key, new MemoryCacheItem(opt.NewValue, opt.NewExpiresIn), opt.NewExpiresIn);
                        success.Add(opt);
                    }
                    else
                    {
                        foreach (var o in success)
                        {
                            memoryCache.Set(o.Key, new MemoryCacheItem(opt.OldValue, opt.NewExpiresIn), opt.NewExpiresIn);
                        }
                        break;
                    }
                }
            }

            return Task.FromResult(success.Count == operations.Length);
        }

        public Task<string> Get(string key)
        {
            if (memoryCache.TryGetValue(key, out var val))
                return Task.FromResult(((MemoryCacheItem)val).Value);
            return Task.FromResult<string>(null);
        }

        public Task<bool> Increament(string key)
        {
            lock (_processLocker)
            {
                if (memoryCache.TryGetValue(key, out var val))
                {
                    var item = (MemoryCacheItem)val;
                    var newVal = long.Parse(item.Value) + 1;
                    var newItem = new MemoryCacheItem(newVal.ToString(), item.AbsoluteExpireDate.Subtract(DateTime.UtcNow));
                    memoryCache.Set(key, newItem, absoluteExpiration: newItem.AbsoluteExpireDate);
                    return Task.FromResult(true);
                }
            }
            return Task.FromResult(false);
        }
    }
}
