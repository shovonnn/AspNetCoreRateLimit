using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AspNetCoreRateLimit.Core;
using Microsoft.Extensions.Options;

namespace AspNetCoreRateLimit
{
    public class IpRateLimitProcessor : IRateLimitProcessor
    {
        private readonly RateLimitOptions _options;
        private readonly IRateLimitCounterStoreAsync _counterStore;
        private readonly IPolicyStore _policyStore;
        private readonly IIpAddressParser _ipParser;
        private readonly RateLimitCore _core;

        private static readonly object _processLocker = new object();

        public IpRateLimitProcessor(IOptions<RateLimitOptions> options,
           IRateLimitCounterStoreAsync counterStore,
           IPolicyStore policyStore,
           IIpAddressParser ipParser)
        {
            _options = options.Value;
            _counterStore = counterStore;
            _policyStore = policyStore;
            _ipParser = ipParser;

            _core = new RateLimitCore(true, options.Value, _counterStore);
        }

        public List<RateLimitRule> GetMatchingRules(ClientRequestIdentity identity)
        {
            var limits = new List<RateLimitRule>();

            // search for rules with IP intervals containing client IP
            var policies = _policyStore.GetIpPolicies(identity.ClientIp);

            if (policies != null && policies.Any())
            {
                var matchPolicies = policies.AsEnumerable();
                var rules = new List<RateLimitRule>();
                foreach (var item in matchPolicies)
                {
                    rules.AddRange(item.Rules);
                }

                if (_options.EnableEndpointRateLimiting)
                {
                    // search for rules with endpoints like "*" and "*:/matching_path"
                    var pathLimits = rules.Where(l => $"*:{identity.Path}".ToLowerInvariant().Contains(l.Endpoint.ToLowerInvariant())).AsEnumerable();
                    limits.AddRange(pathLimits);

                    // search for rules with endpoints like "matching_verb:/matching_path"
                    var verbLimits = rules.Where(l => $"{identity.HttpVerb}:{identity.Path}".ToLowerInvariant().Contains(l.Endpoint.ToLowerInvariant())).AsEnumerable();
                    limits.AddRange(verbLimits);
                }
                else
                {
                    //ignore endpoint rules and search for global rules only
                    var genericLimits = rules.Where(l => l.Endpoint == "*").AsEnumerable();
                    limits.AddRange(genericLimits);
                }
            }

            // get the most restrictive limit for each period 
            limits = limits.GroupBy(l => l.Period).Select(l => l.OrderBy(x => x.Limit)).Select(l => l.First()).ToList();

            // search for matching general rules
            if (_options.IPGeneralRules != null)
            {
                var matchingGeneralLimits = new List<RateLimitRule>();
                if (_options.EnableEndpointRateLimiting)
                {
                    // search for rules with endpoints like "*" and "*:/matching_path" in general rules
                    var pathLimits = _options.IPGeneralRules.Where(l => $"*:{identity.Path}".ToLowerInvariant().Contains(l.Endpoint.ToLowerInvariant())).AsEnumerable();
                    matchingGeneralLimits.AddRange(pathLimits);

                    // search for rules with endpoints like "matching_verb:/matching_path" in general rules
                    var verbLimits = _options.IPGeneralRules.Where(l => $"{identity.HttpVerb}:{identity.Path}".ToLowerInvariant().IsMatch(l.Endpoint.ToLowerInvariant())).AsEnumerable();
                    matchingGeneralLimits.AddRange(verbLimits);
                }
                else
                {
                    //ignore endpoint rules and search for global rules in general rules
                    var genericLimits = _options.IPGeneralRules.Where(l => l.Endpoint == "*").AsEnumerable();
                    matchingGeneralLimits.AddRange(genericLimits);
                }

                // get the most restrictive general limit for each period 
                var generalLimits = matchingGeneralLimits.GroupBy(l => l.Period).Select(l => l.OrderBy(x => x.Limit)).Select(l => l.First()).ToList();

                foreach (var generalLimit in generalLimits)
                {
                    // add general rule if no specific rule is declared for the specified period
                    if (!limits.Exists(l => l.Period == generalLimit.Period))
                    {
                        limits.Add(generalLimit);
                    }
                }
            }

            foreach (var item in limits)
            {
                //parse period text into time spans
                item.PeriodTimespan = _core.ConvertToTimeSpan(item.Period);
            }

            limits = limits.OrderBy(l => l.PeriodTimespan).ToList();
            if (_options.StackBlockedRequests)
            {
                limits.Reverse();
            }

            return limits;
        }

        public bool IsWhitelisted(ClientRequestIdentity requestIdentity)
        {
            if (_options.IpWhitelist != null && _ipParser.ContainsIp(_options.IpWhitelist, requestIdentity.ClientIp))
            {
                return true;
            }

            if (_options.ClientWhitelist != null && _options.ClientWhitelist.Contains(requestIdentity.ClientId))
            {
                return true;
            }

            if (_options.EndpointWhitelist != null && _options.EndpointWhitelist.Any())
            {
                if (_options.EndpointWhitelist.Any(x => $"{requestIdentity.HttpVerb}:{requestIdentity.Path}".ToLowerInvariant().Contains(x.ToLowerInvariant())) ||
                    _options.EndpointWhitelist.Any(x => $"*:{requestIdentity.Path}".ToLowerInvariant().Contains(x.ToLowerInvariant())))
                    return true;
            }

            return false;
        }

        public Task<RateLimitCounter> ProcessRequest(ClientRequestIdentity requestIdentity, RateLimitRule rule)
        {
            return _core.ProcessRequest(requestIdentity, rule);
        }

        public Task<RateLimitHeaders> GetRateLimitHeaders(ClientRequestIdentity requestIdentity, RateLimitRule rule)
        {
            return _core.GetRateLimitHeaders(requestIdentity, rule);
        }

        public string RetryAfterFrom(DateTime timestamp, RateLimitRule rule)
        {
            return _core.RetryAfterFrom(timestamp, rule);
        }
    }
}
