using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AspNetCoreRateLimit
{
    public abstract class RateLimtMiddlewareBase
    {
        private readonly RequestDelegate _next;
        private readonly ILogger _logger;
        private readonly IIpAddressParser _ipParser;
        private readonly IRateLimitProcessor _processor;
        private readonly RateLimitOptions _options;

        public RateLimtMiddlewareBase(RequestDelegate next,
                                      IOptions<RateLimitOptions> options,
                                      ILogger logger,
                                      IRateLimitProcessor processor,
                                      IIpAddressParser ipParser
            )
        {
            _next = next;
            _options = options.Value;
            _logger = logger;
            _ipParser = ipParser;
            _processor = processor;
        }

        public async Task Invoke(HttpContext httpContext)
        {
            // check if rate limiting is enabled
            if (_options == null)
            {
                await _next.Invoke(httpContext);
                return;
            }

            // compute identity from request
            var identity = ParseIdentity(httpContext);

            // check white list
            if (_processor.IsWhitelisted(identity))
            {
                await _next.Invoke(httpContext);
                return;
            }

            if (_processor.IsTemporaryBlocked(identity, out var retryAfterSeconds))
            {
                await ReturnQuotaExceededResponse(httpContext, null, retryAfterSeconds.ToString());
                return;
            }

            var rules = _processor.GetMatchingRules(identity);

            foreach (var rule in rules)
            {
                if (rule.Limit > 0)
                {
                    // increment counter
                    var counter = await _processor.ProcessRequest(identity, rule);

                    // check if key expired
                    if (counter.Timestamp + rule.PeriodTimespan.Value < DateTime.UtcNow)
                    {
                        continue;
                    }

                    // check if limit is reached
                    if (counter.TotalRequests > rule.Limit)
                    {
                        //compute retry after value
                        var retryAfter = _processor.RetryAfterFrom(counter.Timestamp, rule);

                        _processor.AddToTemporaryBlocked(identity, TimeSpan.FromSeconds(double.Parse(retryAfter)));
                        // log blocked request
                        LogBlockedRequest(httpContext, identity, counter, rule);

                        // break execution
                        await ReturnQuotaExceededResponse(httpContext, rule, retryAfter);
                        return;
                    }
                }
            }

            //set X-Rate-Limit headers for the longest period
            if (rules.Any() && !_options.DisableRateLimitHeaders)
            {
                var rule = rules.OrderByDescending(x => x.PeriodTimespan.Value).First();
                var headers = await _processor.GetRateLimitHeaders(identity, rule);
                headers.Context = httpContext;

                httpContext.Response.OnStarting(SetRateLimitHeaders, state: headers);
            }

            await _next.Invoke(httpContext);
        }

        public virtual ClientRequestIdentity ParseIdentity(HttpContext httpContext)
        {
            var clientId = "anon";
            if (httpContext.Request.Headers.Keys.Contains(_options.ClientIdHeader, StringComparer.CurrentCultureIgnoreCase))
            {
                clientId = httpContext.Request.Headers[_options.ClientIdHeader].First();
            }

            var clientIp = string.Empty;
            try
            {
                var ip = _ipParser.GetClientIp(httpContext);
                if (ip == null)
                {
                    throw new Exception("RateLimitMiddleware can't parse caller IP");
                }

                clientIp = ip.ToString();
            }
            catch (Exception ex)
            {
                throw new Exception("RateLimitMiddleware can't parse caller IP", ex);
            }

            return new ClientRequestIdentity
            {
                ClientIp = clientIp,
                Path = httpContext.Request.Path.ToString().ToLowerInvariant(),
                HttpVerb = httpContext.Request.Method.ToLowerInvariant(),
                ClientId = clientId
            };
        }

        public virtual Task ReturnQuotaExceededResponse(HttpContext httpContext, RateLimitRule rule, string retryAfter)
        {
            var message = string.IsNullOrEmpty(_options.QuotaExceededMessage) && rule != null ? $"API calls quota exceeded! maximum admitted {rule.Limit} per {rule.Period}." : _options.QuotaExceededMessage;

            if (!_options.DisableRateLimitHeaders)
            {
                httpContext.Response.Headers["Retry-After"] = retryAfter;
            }

            httpContext.Response.StatusCode = _options.HttpStatusCode;
            return httpContext.Response.WriteAsync(message ?? "API calls quota exceeded!");
        }

        public virtual void LogBlockedRequest(HttpContext httpContext, ClientRequestIdentity identity, RateLimitCounter counter, RateLimitRule rule)
        {
            _logger.LogInformation($"Request {identity.HttpVerb}:{identity.Path} from IP {identity.ClientIp} has been blocked, quota {rule.Limit}/{rule.Period} exceeded by {counter.TotalRequests}. Blocked by rule {rule.Endpoint}, TraceIdentifier {httpContext.TraceIdentifier}.");
        }

        private Task SetRateLimitHeaders(object rateLimitHeaders)
        {
            var headers = (RateLimitHeaders)rateLimitHeaders;

            headers.Context.Response.Headers["X-Rate-Limit-Limit"] = headers.Limit;
            headers.Context.Response.Headers["X-Rate-Limit-Remaining"] = headers.Remaining;
            headers.Context.Response.Headers["X-Rate-Limit-Reset"] = headers.Reset;

            return Task.CompletedTask;
        }
    }
}
