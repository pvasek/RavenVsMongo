using System;
using System.Collections.Generic;
using System.Linq;
using RavenVsMongo.Utils;

namespace RavenVsMongo
{
    class Program
    {
        public static void Main(string[] args)
        {
            NConsoler.Consolery.Run(typeof(Program), args);            
        }

        [NConsoler.Action]
        public static void Run(
            [NConsoler.Optional("Both", Description = "What you want to test: Both, Raven, Mongo")] string testMode,
            [NConsoler.Optional(1000, Description = "Number of items that should be generated")] int itemsCount,
            [NConsoler.Optional(30000, Description = "Wait after generating items in ms (time for rebuilding indexes)")] int waitAfterGeneratingMs)
        {
            TestSettings.WaitForRebuildIndexesMs = waitAfterGeneratingMs;
            TestSettings.TestMode = (TestMode)Enum.Parse(typeof(TestMode), testMode);
            TestSettings.GenerateItemsCount = itemsCount;

            // 100 ~ 51kB, 250 ~ 122kB, 500 ~ 239kB, 1000 ~ 475kB, 1500 ~ 710kB - document size, size ~ 4.22KB + count*0.46
            var list = new List<Result>();
            list.Add(RunRound(0));
            list.Add(RunRound(10));
            list.Add(RunRound(20));
            list.Add(RunRound(50));
            list.Add(RunRound(100));
            list.Add(RunRound(250));
            list.Add(RunRound(500));
            list.Add(RunRound(1000));
            list.Add(RunRound(1500));
            PrintResults(list);
            Console.WriteLine();
            Console.WriteLine("Press enter to close the app.");
            Console.ReadLine();
        }

        private static void PrintResults(List<Result> list)
        {
            PrintHeader();
            foreach (var result in list)
            {
                if (result.Raven != null)
                {
                    PrintLine(result.DocumentSize, result.Raven, "Raven");
                }
                if (result.Mongo != null)
                {
                    PrintLine(result.DocumentSize, result.Mongo, "Mongo");
                }
            }
        }

        private static void PrintHeader()
        {
            var cols = new List<object>();
            cols.Add("Type");
            cols.Add("Document Size [kB]");
            cols.Add("Read item avg [ms]");
            cols.Add("Read filtered item avg [ms]");
            cols.Add("Write item avg [ms]");
            cols.Add("Read count [ms]");
            
            Console.WriteLine(String.Join(",", cols));
        }

        private static void PrintLine(decimal documentSize, TestResultSet result, string name)
        {
            var cols = new List<object>();
            cols.Add(name);
            cols.Add(documentSize);
            cols.Add(result.Read.ItemAvgMs);
            cols.Add(result.Categories.Average(i => i.ItemAvgMs));
            cols.Add(result.Write.ItemAvgMs);
            cols.Add(result.CountItemsMs);

            Console.WriteLine(String.Join(",", cols));
        }

        private static Result RunRound(int words)
        {
            Console.WriteLine();
            Console.WriteLine("RUNNING ROUND - {0} ##################################################", words);
            Console.WriteLine();

            PersonGenerator.TextPropertyWordCount = words;// 100 ~ 51kB, 250 ~ 122kB, 500 ~ 239kB, 1000 ~ 475kB, 1500 ~ 710kB - document size
            TestResultSet ravenResult = null;
            if (TestSettings.TestRaven)
            {
                Console.WriteLine("RAVEN #########");

                GC.Collect();
                ravenResult = RavenTest.Run(readCount: 500, generateCount: TestSettings.GenerateItemsCount, bulkSize: 5000);
            }

            TestResultSet mongoResult = null;
            if (TestSettings.TestMongo)
            {
                Console.WriteLine();
                Console.WriteLine("MONGO #########");

                GC.Collect();
                mongoResult = MongoTest.Run(readCount: 500, generateCount: TestSettings.GenerateItemsCount, bulkSize: 5000);
            }
            return new Result {WordCount = words, Raven = ravenResult, Mongo = mongoResult};
        }

        private class Result
        {
            public decimal DocumentSize { get { return  4.22m + WordCount*0.46m; }}
            public int WordCount;
            public TestResultSet Raven;
            public TestResultSet Mongo;
        }
    }
}
