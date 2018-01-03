using System;
namespace AspNetCoreRateLimit
{
    public struct AtomicReplaceOperation
    {
        public string Key { get; set; }
        public object NewValue { get; set; }
        public object OldValue { get; set; }
        public TimeSpan NewExpiresIn { get; set; }
    }
}
