using System.Collections.Generic;
namespace AspNetCoreRateLimit
{
    public interface IPolicyStore
    {
        List<RateLimitPolicy> GetClientPolicies(string clientId);
        List<RateLimitPolicy> GetIpPolicies(string clientIp);
    }
}