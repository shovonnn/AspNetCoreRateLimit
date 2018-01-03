using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using StackExchange.Redis;
using Microsoft.AspNetCore.Builder;

namespace AspNetCoreRateLimit
{
    public static class StartupExtensions
    {
        public static void AddRateLimiting(this IServiceCollection services, IConfiguration config)
        {
            services.AddSingleton<IRateLimitCounterStoreAsync, RateLimitCounterStoreAsync>();
            services.AddMemoryCache();
            if (config.GetSection("IpRateLimiting").Value != null)
            {
                services.Configure<IpRateLimitOptions>(config.GetSection("IpRateLimiting"));

                if (config.GetSection("IpRateLimitPolicies").Value != null)
                    services.Configure<IpRateLimitPolicies>(config.GetSection("IpRateLimitPolicies"));

                // inject counter and rules stores
                services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();
            }
            if (config.GetSection("ClientRateLimiting").Value != null)
            {
                services.Configure<ClientRateLimitOptions>(config.GetSection("ClientRateLimiting"));

                if (config.GetSection("ClientRateLimitPolicies").Value != null)
                    services.Configure<ClientRateLimitPolicies>(config.GetSection("ClientRateLimitPolicies"));

                // inject counter and rules stores
                services.AddSingleton<IClientPolicyStore, MemoryCacheClientPolicyStore>();
            }
        }
        public static void AddMemoryBasedRateLimit(this IServiceCollection services, IConfiguration config)
        {
            services.AddRateLimiting(config);
            services.AddScoped<ICacheClient, MemoryCacheClient>();
        }
        public static void AddRedisBasedRateLimit(this IServiceCollection services, IConfiguration config, Func<ConnectionMultiplexer> redisFactory)
        {
            services.AddRateLimiting(config);
            services.AddScoped<ICacheClient>((arg) => new RedisCacheClient(redisFactory.Invoke()));
        }

        public static IApplicationBuilder UseIpRateLimiting(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<IpRateLimitMiddleware>();
        }

        public static IApplicationBuilder UseClientRateLimiting(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ClientRateLimitMiddleware>();
        }
    }
}
