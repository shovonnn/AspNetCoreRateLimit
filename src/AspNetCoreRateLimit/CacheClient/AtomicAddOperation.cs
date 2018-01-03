using System;
namespace AspNetCoreRateLimit
{
    public struct AtomicAddOperation
    {
        public string Key { get; set; }
        public object Value { get; set; }
        public TimeSpan ExpiresIn { get; set; }
    }
}
