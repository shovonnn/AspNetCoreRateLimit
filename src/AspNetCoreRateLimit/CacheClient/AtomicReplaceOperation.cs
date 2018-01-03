using System;
namespace AspNetCoreRateLimit
{
    public struct AtomicReplaceOperation
    {
        public string Key { get; set; }
        public string NewValue { get; set; }
        public string OldValue { get; set; }
        public TimeSpan NewExpiresIn { get; set; }
        public AtomicReplaceOperation(string key, string newValue, string oldValue, TimeSpan newExpiresIn)
        {
            Key = key;
            NewValue = newValue;
            OldValue = oldValue;
            NewExpiresIn = newExpiresIn;
        }
    }
}
