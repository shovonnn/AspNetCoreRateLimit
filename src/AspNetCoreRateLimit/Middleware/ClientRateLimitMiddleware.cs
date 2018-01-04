using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AspNetCoreRateLimit
{
    public class ClientRateLimitMiddleware : RateLimtMiddlewareBase
    {
        public ClientRateLimitMiddleware(RequestDelegate next,
                                         IOptions<RateLimitOptions> options,
                                           ILogger<ClientRateLimitMiddleware> logger,
                                         ClientRateLimitProcessor processor,
                                         IIpAddressParser ipParser
                                        ) : base(next, options, logger, processor, ipParser)
        {
        }
    }
}
