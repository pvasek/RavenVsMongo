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
            [NConsoler.Required(Description = "Output CSV file name")] string outputFile,
            [NConsoler.Optional("Both", Description = "What you want to test: Both, Raven, Mongo")] string testMode,
            [NConsoler.Optional("100, 1000, 10000, 100000", Description = "Number of items that should be generated comma separated")] string itemsCounts,
            [NConsoler.Optional("1, 50, 100, 250, 500", Description = "Document sizes that should be generated comma separated")] string documentSizes,
            [NConsoler.Optional(60000, Description = "Wait after generating items in ms (time for rebuilding indexes)")] int waitAfterGeneratingMs,
            [NConsoler.Optional(60000, Description = "Wait between reading items in ms")] int waitBetweenReadsMs,
            [NConsoler.Optional("All", Description = "The mode of the test All, Read")] string testType)
        {
            TestSettings.WaitForRebuildIndexesMs = waitAfterGeneratingMs;
            TestSettings.TestMode = (TestMode)Enum.Parse(typeof(TestMode), testMode);
            TestSettings.TestType = (TestType)Enum.Parse(typeof(TestType), testType);
            TestSettings.NumberOfCategoriesTested = 3;
            TestSettings.OutputFile = outputFile;
            TestSettings.PauseBetweenReadsMs = waitBetweenReadsMs;

            var couns = itemsCounts.Split(',').Select(i => Int32.Parse(i.Trim())).ToArray();
            var sizes = documentSizes.Split(',').Select(i => Int32.Parse(i.Trim())).ToArray();
            RunRounds(couns, sizes);
        }


        private static void RunRounds(int[] itemSizes, int[] documentSizes)
        {
            var list = new List<Result>();
            foreach (var itemSize in itemSizes)
            {
                foreach (var documentSize in documentSizes)
                {
                    list.Add(RunRound(itemSize, documentSize));
                    ResultCsvWriter.WriteToFile(TestSettings.OutputFile, list);
                }
            }
        }

        private static Result RunRound(int itemsCount, int documentSizeKb)
        {
            Console.WriteLine();
            Console.WriteLine("ROUND - ItemsCount: {0}, DocumentSize: {1} kB ##################################################", itemsCount, documentSizeKb);
            Console.WriteLine();

            PersonGenerator.ItemSizeKb = documentSizeKb;
            TestResultSet ravenResult = null;
            string databaseName = string.Format("test_count_{0}_doc_size_{1}", itemsCount, documentSizeKb);
            if (TestSettings.TestRaven)
            {
                Console.WriteLine("RAVEN #########");

                GC.Collect();

                if (TestSettings.TestType == TestType.All)
                {
                    ravenResult = RavenTest.Run(databaseName, readCount: 1000, generateCount: itemsCount, bulkSize: 5000);
                }
                else
                {
                    ravenResult = RavenTest.RunReadTest(databaseName, readCount: 1000);
                }
            }

            TestResultSet mongoResult = null;
            if (TestSettings.TestMongo)
            {
                Console.WriteLine();
                Console.WriteLine("MONGO #########");

                GC.Collect();
                mongoResult = MongoTest.Run(databaseName, readCount: 1000, generateCount: itemsCount, bulkSize: 5000);
            }
            return new Result
                {
                    ItemsCount = itemsCount,
                    DocumentSizeKb = documentSizeKb, 
                    Raven = ravenResult, 
                    Mongo = mongoResult
                };
        }
    }
}
