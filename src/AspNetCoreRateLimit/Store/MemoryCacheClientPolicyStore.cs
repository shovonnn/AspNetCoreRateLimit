﻿using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;

namespace AspNetCoreRateLimit
{
    public class MemoryCacheClientPolicyStore: IClientPolicyStore
    {
        private readonly IMemoryCache _memoryCache;

        public MemoryCacheClientPolicyStore(IMemoryCache memoryCache, 
            IOptions<RateLimitOptions> options = null, 
            IOptions<ClientRateLimitPolicies> policies = null)
        {
            _memoryCache = memoryCache;

            //save client rules defined in appsettings in cache on startup
            if(options != null && options.Value != null && policies != null && policies.Value != null && policies.Value.ClientRules != null)
            {
                foreach (var rule in policies.Value.ClientRules)
                {
                    Set($"{options.Value.ClientPolicyPrefix}_{rule.ClientId}", new ClientRateLimitPolicy { ClientId = rule.ClientId, Rules = rule.Rules });
                }
            }
        }

        public void Set(string id, ClientRateLimitPolicy policy)
        {
            _memoryCache.Set(id, policy, new MemoryCacheEntryOptions().SetPriority(CacheItemPriority.NeverRemove));
        }

        public bool Exists(string id)
        {
            ClientRateLimitPolicy stored;
            return _memoryCache.TryGetValue(id, out stored);
        }

        public ClientRateLimitPolicy Get(string id)
        {
            ClientRateLimitPolicy stored;
            if (_memoryCache.TryGetValue(id, out stored))
            {
                return stored;
            }

            return null;
        }

        public void Remove(string id)
        {
            _memoryCache.Remove(id);
        }
    }
}
