using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;

namespace AspNetCoreRateLimit
{
    public class ClientRateLimitProcessor : RateLimitProcessorBase
    {
        private readonly RateLimitOptions _options;
        private readonly IRateLimitCounterStoreAsync _counterStore;
        private readonly IPolicyStore _policyStore;

        public ClientRateLimitProcessor(IOptions<RateLimitOptions> options,
           IRateLimitCounterStoreAsync counterStore,
                                        ITemporaryBlockStore blockListStore,
                                        IPolicyStore policyStore) : base(false, options.Value, blockListStore, counterStore)
        {
            _options = options.Value;
            _counterStore = counterStore;
            _policyStore = policyStore;
        }

        public override List<RateLimitRule> GetMatchingRules(ClientRequestIdentity identity)
        {
            var limits = new List<RateLimitRule>();
            var policy = _policyStore.GetClientPolicies(identity.ClientId);

            if (policy != null)
            {
                var rules = policy.SelectMany(_ => _.Rules).AsEnumerable();
                if (_options.EnableEndpointRateLimiting)
                {
                    // search for rules with endpoints like "*" and "*:/matching_path"
                    var pathLimits = rules.Where(l => $"*:{identity.Path}".ContainsIgnoreCase(l.Endpoint)).AsEnumerable();
                    limits.AddRange(pathLimits);

                    // search for rules with endpoints like "matching_verb:/matching_path"
                    var verbLimits = rules.Where(l => $"{identity.HttpVerb}:{identity.Path}".ContainsIgnoreCase(l.Endpoint)).AsEnumerable();
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
            if (_options.ClientGeneralRules != null)
            {
                var matchingGeneralLimits = new List<RateLimitRule>();
                if (_options.EnableEndpointRateLimiting)
                {
                    // search for rules with endpoints like "*" and "*:/matching_path" in general rules
                    var pathLimits = _options.ClientGeneralRules.Where(l => $"*:{identity.Path}".ContainsIgnoreCase(l.Endpoint)).AsEnumerable();
                    matchingGeneralLimits.AddRange(pathLimits);

                    // search for rules with endpoints like "matching_verb:/matching_path" in general rules
                    var verbLimits = _options.ClientGeneralRules.Where(l => $"{identity.HttpVerb}:{identity.Path}".ContainsIgnoreCase(l.Endpoint)).AsEnumerable();
                    matchingGeneralLimits.AddRange(verbLimits);
                }
                else
                {
                    //ignore endpoint rules and search for global rules in general rules
                    var genericLimits = _options.ClientGeneralRules.Where(l => l.Endpoint == "*").AsEnumerable();
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
                item.PeriodTimespan = ConvertToTimeSpan(item.Period);
            }

            limits = limits.OrderBy(l => l.PeriodTimespan).ToList();
            if (_options.StackBlockedRequests)
            {
                limits.Reverse();
            }

            return limits;
        }

        public override bool IsWhitelisted(ClientRequestIdentity requestIdentity)
        {
            if (_options.ClientWhitelist != null && _options.ClientWhitelist.Contains(requestIdentity.ClientId))
            {
                return true;
            }

            if (_options.EndpointWhitelist != null && _options.EndpointWhitelist.Any())
            {
                if (_options.EndpointWhitelist.Any(x => $"{requestIdentity.HttpVerb}:{requestIdentity.Path}".ContainsIgnoreCase(x)) ||
                    _options.EndpointWhitelist.Any(x => $"*:{requestIdentity.Path}".ContainsIgnoreCase(x)))
                    return true;
            }

            return false;
        }
    }
}
