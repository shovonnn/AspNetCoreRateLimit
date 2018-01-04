using System;
using System.Globalization;
using System.Threading.Tasks;

namespace AspNetCoreRateLimit
{
    public class RateLimitCore
    {
        private readonly RateLimitOptions _options;
        private readonly IRateLimitCounterStoreAsync _counterStore;
        private readonly bool _ipRateLimiting;

        private static readonly object _processLocker = new object();

        public RateLimitCore(bool ipRateLimiting,
            RateLimitOptions options,
                             IRateLimitCounterStoreAsync counterStore)
        {
            _ipRateLimiting = ipRateLimiting;
            _options = options;
            _counterStore = counterStore;
        }

        public string ComputeCounterKey(ClientRequestIdentity requestIdentity, RateLimitRule rule)
        {
            var key = _ipRateLimiting ?
                $"{_options.RateLimitCounterPrefix}_{requestIdentity.ClientIp}_{rule.Period}" :
                $"{_options.RateLimitCounterPrefix}_{requestIdentity.ClientId}_{rule.Period}";

            if (_options.EnableEndpointRateLimiting)
            {
                key += $"_{requestIdentity.HttpVerb}_{requestIdentity.Path}";

                // TODO: consider using the rule endpoint as key, this will allow to rate limit /api/values/1 and api/values/2 under same counter
                //key += $"_{rule.Endpoint}";
            }

            var idBytes = System.Text.Encoding.UTF8.GetBytes(key);

            byte[] hashBytes;

            using (var algorithm = System.Security.Cryptography.SHA1.Create())
            {
                hashBytes = algorithm.ComputeHash(idBytes);
            }

            return BitConverter.ToString(hashBytes).Replace("-", string.Empty);
        }

        public async Task<RateLimitCounter> ProcessRequest(ClientRequestIdentity requestIdentity, RateLimitRule rule, int retryAttempt = 0)
        {

            var counterId = ComputeCounterKey(requestIdentity, rule);

            var entry = await _counterStore.GetAsync(counterId);
            if (entry.HasValue)
            {
                // entry has not expired
                if (entry.Value.Timestamp + rule.PeriodTimespan.Value >= DateTime.UtcNow)
                {
                    // increment request count
                    return await _counterStore.IncrementAsync(counterId, entry.Value);
                }
                try
                {
                    return await _counterStore.ResetCounter(counterId, entry.Value, rule.PeriodTimespan.Value);
                }
                catch (ConcurrencyException)
                {
                    if (retryAttempt < 3)
                        return await ProcessRequest(requestIdentity, rule, retryAttempt + 1);
                    else
                        return _maxxedOutCounter();
                }
            }
            try
            {
                return await _counterStore.CreateCounter(counterId, rule.PeriodTimespan.Value);
            }
            catch (ConcurrencyException)
            {
                if (retryAttempt < 3)
                    return await ProcessRequest(requestIdentity, rule, retryAttempt + 1);
                else
                    return _maxxedOutCounter();
            }
        }

        static RateLimitCounter _maxxedOutCounter() => new RateLimitCounter() { Timestamp = DateTime.UtcNow, TotalRequests = int.MaxValue };

        public async Task<RateLimitHeaders> GetRateLimitHeaders(ClientRequestIdentity requestIdentity, RateLimitRule rule)
        {
            var headers = new RateLimitHeaders();
            var counterId = ComputeCounterKey(requestIdentity, rule);
            var entry = await _counterStore.GetAsync(counterId);
            if (entry.HasValue)
            {
                headers.Reset = (entry.Value.Timestamp + ConvertToTimeSpan(rule.Period)).ToUniversalTime().ToString("o", DateTimeFormatInfo.InvariantInfo);
                headers.Limit = rule.Period;
                headers.Remaining = (rule.Limit - entry.Value.TotalRequests).ToString();
            }
            else
            {
                headers.Reset = (DateTime.UtcNow + ConvertToTimeSpan(rule.Period)).ToUniversalTime().ToString("o", DateTimeFormatInfo.InvariantInfo);
                headers.Limit = rule.Period;
                headers.Remaining = rule.Limit.ToString();
            }

            return headers;
        }

        public string RetryAfterFrom(DateTime timestamp, RateLimitRule rule)
        {
            var diff = (timestamp + rule.PeriodTimespan.Value) - DateTime.UtcNow;
            var diffInSeconds = Math.Max(diff.TotalSeconds, 1);
            return $"{diffInSeconds:F0}";
        }

        public TimeSpan ConvertToTimeSpan(string timeSpan)
        {
            var l = timeSpan.Length - 1;
            var value = timeSpan.Substring(0, l);
            var type = timeSpan.Substring(l, 1);

            switch (type)
            {
                case "d": return TimeSpan.FromDays(double.Parse(value));
                case "h": return TimeSpan.FromHours(double.Parse(value));
                case "m": return TimeSpan.FromMinutes(double.Parse(value));
                case "s": return TimeSpan.FromSeconds(double.Parse(value));
                default: throw new FormatException($"{timeSpan} can't be converted to TimeSpan, unknown type {type}");
            }
        }
    }
}
