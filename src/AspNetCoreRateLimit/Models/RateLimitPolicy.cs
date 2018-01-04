using System.Collections.Generic;

namespace AspNetCoreRateLimit
{
    public class RateLimitPolicy
    {
        public string ClientId { get; set; }
        public string Ip { get; set; }
        public List<RateLimitRule> Rules { get; set; }
    }
}
