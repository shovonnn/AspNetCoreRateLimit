﻿using System;
using System.Threading.Tasks;
using StackExchange.Redis;
namespace AspNetCoreRateLimit
{
    public class RedisCacheClient : ICacheClient
    {

        readonly IDatabase Database;
        private static readonly RedisValue _nullValue = "@@NULL";

        public RedisCacheClient(ConnectionMultiplexer _connection)
        {
            Database = _connection.GetDatabase();
        }

        public async Task<bool> AtomicAdd(params AtomicAddOperation[] operations)
        {
            var trans = Database.CreateTransaction();
            foreach (var opt in operations)
            {
                trans.AddCondition(Condition.KeyNotExists(opt.Key));
                trans.StringSetAsync(opt.Key, opt.Value, opt.ExpiresIn);
            }
            return await trans.ExecuteAsync();
        }

        public async Task<bool> AtomicReplace(params AtomicReplaceOperation[] operations)
        {
            var trans = Database.CreateTransaction();
            foreach (var opt in operations)
            {
                trans.AddCondition(Condition.StringEqual(opt.Key, opt.OldValue));
                trans.StringSetAsync(opt.Key, opt.NewValue, opt.NewExpiresIn);
            }
            return await trans.ExecuteAsync();
        }

        public async Task<string> Get(string key)
        {
            var value = await Database.StringGetAsync(key);
            if (!value.HasValue || value.IsNull || value == _nullValue) return null;
            return value;
        }

        public async Task Increament(string key)
        {
            await Database.StringIncrementAsync(key);
        }
    }
}
