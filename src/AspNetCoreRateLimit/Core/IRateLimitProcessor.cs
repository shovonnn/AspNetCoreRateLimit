using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AspNetCoreRateLimit
{
    public interface IRateLimitProcessor
    {
        List<RateLimitRule> GetMatchingRules(ClientRequestIdentity identity);
        Task<RateLimitHeaders> GetRateLimitHeaders(ClientRequestIdentity requestIdentity, RateLimitRule rule);
        bool IsWhitelisted(ClientRequestIdentity requestIdentity);
        Task<RateLimitCounter> ProcessRequest(ClientRequestIdentity requestIdentity, RateLimitRule rule);
        string RetryAfterFrom(DateTime timestamp, RateLimitRule rule);
    }
}