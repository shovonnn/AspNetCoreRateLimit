using System;
namespace AspNetCoreRateLimit
{
    public struct TemporaryBlocked
    {
        public DateTime Timestamp { get; set; }
        public TimeSpan Duration { get; set; }
    }
}
