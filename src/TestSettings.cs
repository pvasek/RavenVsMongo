using System;
using System.Threading;

namespace RavenVsMongo
{
    public static class TestSettings
    {
        public static int WaitForRebuildIndexesMs { get; set; }
        public static TestMode TestMode { get; set; }
        public static int NumberOfCategoriesTested { get; set; }
        public static string OutputFile { get; set; }
        public static int PauseBetweenReadsMs { get; set; }
        public static TestType TestType { get; set; }

        public static bool TestRaven { get { return (TestMode & TestMode.Raven) == TestMode.Raven; } }
        public static bool TestMongo { get { return (TestMode & TestMode.Mongo) == TestMode.Mongo; } }

        public static void PauseBetweenReads()
        {
            Console.WriteLine("Waiting between reads...");
            Thread.Sleep(PauseBetweenReadsMs);
        }
    }
}