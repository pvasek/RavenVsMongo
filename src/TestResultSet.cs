using System.Collections.Generic;

namespace RavenVsMongo
{
    public class TestResultSet
    {
        public TestResultSet()
        {
            Write = new TestResult();
            Read = new TestResult();
            ReadRepeated = new TestResult();
            Categories = new List<TestResult>();
        }

        public long DeletingItemsMs { get; set; }
        public long CountItemsMs { get; set; }
        public TestResult Write { get; set; }
        public TestResult Read { get; set; }
        public TestResult ReadRepeated { get; set; }
        public List<TestResult> Categories { get; private set; }

    }

    public class TestResult
    {
        public int? Count { get; set; }
        public long TotalMs { get; set; }
        public long? ItemAvgMs
        {
            get
            {
                if (Count == null)
                    return null;

                return TotalMs / Count;
            }
        }       
    }
}