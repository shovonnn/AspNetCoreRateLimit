using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AspNetCoreRateLimit
{
    public class MemoryCachePolicyStore : IPolicyStore
    {
        private readonly IMemoryCache _memoryCache;

        List<RateLimitPolicy> policies;
        public MemoryCachePolicyStore(IMemoryCache memoryCache,
            IOptions<RateLimitOptions> options = null)
        {
            _memoryCache = memoryCache;
            policies = options.Value?.Policies ?? new List<RateLimitPolicy>();
        }

        public List<RateLimitPolicy> GetClientPolicies(string clientId)
        {
            return policies.Where(policy => policy.ClientId != null && policy.ClientId == clientId).ToList();
        }

        public List<RateLimitPolicy> GetIpPolicies(string clientIp)
        {
            return policies.Where(policy => policy.Ip != null && policy.Ip == clientIp).ToList();
        }
    }
}
