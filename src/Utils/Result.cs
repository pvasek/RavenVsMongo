namespace RavenVsMongo.Utils
{
    public class Result
    {
        public int ItemsCount { get; set; }
        public int DocumentSizeKb { get; set; }
        public TestResultSet Raven { get; set; }
        public TestResultSet Mongo { get; set; }
    }
}