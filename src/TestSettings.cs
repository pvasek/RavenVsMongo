namespace RavenVsMongo
{
    public static class TestSettings
    {
        public static int WaitForRebuildIndexesMs { get; set; }
        public static TestMode TestMode { get; set; }
        public static int GenerateItemsCount { get; set; }

        public static bool TestRaven { get { return (TestMode & TestMode.Raven) == TestMode.Raven; } }
        public static bool TestMongo { get { return (TestMode & TestMode.Mongo) == TestMode.Mongo; } }
    }
}