using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Threading.Tasks;


namespace AspNetCoreRateLimit
{
    public class IpRateLimitMiddleware : RateLimtMiddlewareBase
    {
        public IpRateLimitMiddleware(RequestDelegate next,
                                         IOptions<RateLimitOptions> options,
                                           ILogger<ClientRateLimitMiddleware> logger,
                                     IpRateLimitProcessor processor,
                                         IIpAddressParser ipParser
                                        ) : base(next, options, logger, processor, ipParser)
        {
        }
    }
}
