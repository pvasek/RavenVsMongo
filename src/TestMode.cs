using System;

namespace RavenVsMongo
{
    [Flags]
    public enum TestMode
    {
        Raven = 1,
        Mongo = 2,
        Both = Raven | Mongo
    }
}