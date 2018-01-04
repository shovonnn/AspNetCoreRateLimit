using System;
namespace AspNetCoreRateLimit
{
    public interface ITemporaryBlockStore
    {
        TemporaryBlocked? Get(string requestKey);
        void Add(string requestKey, TimeSpan duration);
    }
}
