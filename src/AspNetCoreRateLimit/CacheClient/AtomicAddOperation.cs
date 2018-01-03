using System;
namespace AspNetCoreRateLimit
{
    public struct AtomicAddOperation
    {
        public string Key { get; set; }
        public string Value { get; set; }
        public TimeSpan ExpiresIn { get; set; }
        public AtomicAddOperation(string key, string value, TimeSpan expiresIn)
        {
            Key = key;
            Value = value;
            ExpiresIn = expiresIn;
        }
    }
}
