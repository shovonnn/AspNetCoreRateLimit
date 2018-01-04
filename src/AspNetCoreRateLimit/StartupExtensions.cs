using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using StackExchange.Redis;
using Microsoft.AspNetCore.Builder;

namespace AspNetCoreRateLimit
{
    public static class StartupExtensions
    {
        public static void AddRateLimiting(this IServiceCollection services, IConfigurationSection config)
        {
            services.AddSingleton<IRateLimitCounterStoreAsync, RateLimitCounterStoreAsync>();
            services.AddMemoryCache();
            services.Configure<RateLimitOptions>(config);
            services.Configure<IpRateLimitPolicies>(config.GetSection("IpPolicies"));
            services.Configure<ClientRateLimitPolicies>(config.GetSection("ClientPolicies"));

            // inject counter and rules stores
            services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();

            // inject counter and rules stores
            services.AddSingleton<IClientPolicyStore, MemoryCacheClientPolicyStore>();

            services.AddTransient<IpRateLimitProcessor>();
            services.AddTransient<ClientRateLimitProcessor>();
            services.AddTransient<IIpAddressParser, ReversProxyIpParser>();
        }
        public static void AddMemoryBasedRateLimit(this IServiceCollection services, IConfigurationSection config)
        {
            services.AddRateLimiting(config);
            services.AddSingleton<ICacheClient, MemoryCacheClient>();
        }
        public static void AddRedisBasedRateLimit(this IServiceCollection services, IConfigurationSection config, Func<ConnectionMultiplexer> redisFactory)
        {
            services.AddRateLimiting(config);
            services.AddSingleton<ICacheClient>((arg) => new RedisCacheClient(redisFactory.Invoke()));
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
