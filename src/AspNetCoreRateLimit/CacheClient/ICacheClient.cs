using System.Threading.Tasks;
namespace AspNetCoreRateLimit
{
    public interface ICacheClient
    {
        Task<string> Get(string key);
        Task<bool> Increament(string key);
        Task<bool> AtomicAdd(params AtomicAddOperation[] operations);
        Task<bool> AtomicReplace(params AtomicReplaceOperation[] operations);
    }
}
