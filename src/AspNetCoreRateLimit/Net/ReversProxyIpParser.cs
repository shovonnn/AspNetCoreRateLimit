using System;
using System.Linq;
using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace AspNetCoreRateLimit
{
    public class ReversProxyIpParser : RemoteIpParser
    {
        private readonly string _realIpHeader;

        public ReversProxyIpParser(IOptions<RateLimitOptions> options)
        {
            _realIpHeader = options.Value.RealIpHeader;
        }

        public override IPAddress GetClientIp(HttpContext context)
        {
            if (context.Request.Headers.Keys.Contains(_realIpHeader, StringComparer.CurrentCultureIgnoreCase))
            {
                return ParseIp(context.Request.Headers[_realIpHeader].Last());
            }

            return base.GetClientIp(context);
        }
    }
}
