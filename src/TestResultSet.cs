using System.Collections.Generic;

namespace RavenVsMongo
{
    public class TestResultSet
    {
        public TestResultSet()
        {
            Write = new TestResult();
            Read1 = new TestResult();
            Read2 = new TestResult();
            Read3 = new TestResult();
            Categories = new List<TestResult>();
        }

        public long DeletingItemsMs { get; set; }
        public long CountItemsMs { get; set; }
        public TestResult Write { get; set; }
        public TestResult Read1 { get; set; }
        public TestResult Read2 { get; set; }
        public TestResult Read3 { get; set; }
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
                if (Count == null || Count == 0)
                    return 0;

                return TotalMs / Count;
            }
        }       
    }
}