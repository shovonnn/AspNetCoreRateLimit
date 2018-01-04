using System;
namespace AspNetCoreRateLimit
{
    public class ConcurrencyException : Exception
    {
        public ConcurrencyException()
        {
        }
    }
}
