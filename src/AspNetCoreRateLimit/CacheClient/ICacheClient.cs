using System.Threading.Tasks;
namespace AspNetCoreRateLimit
{
    public interface ICacheClient
    {
        Task<T> Get<T>(string key);
        Task Increament(string key);
        Task<bool> AtomicAdd(params AtomicAddOperation[] operations);
        Task<bool> AtomicReplace(params AtomicReplaceOperation[] operations);
    }
}
