using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Raven.Abstractions.Data;
using Raven.Client.Document;
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
        public static void Test(
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

        [NConsoler.Action]
        public static void ReadTest([NConsoler.Required(Description = "The test database to read from")] string dbName)
        {
            const int readCount = 1000;
            RavenTest.RunReadTest(dbName, readCount);
        }

        [NConsoler.Action]
        public static void OrenWay([NConsoler.Required(Description = "The test database to read from")]string dbName)
        {
            var list = new List<string>();

            using (var store = new DocumentStore
            {
                Url = "http://localhost:8080",
                DefaultDatabase = dbName,
            }.Initialize())
            {
                
                var sp = Stopwatch.StartNew();
                int start = 0;
                while (true)
                {
                    var result = store.DatabaseCommands.Query("PersonList", new IndexQuery
                    {
                        FieldsToFetch = new[] { Constants.DocumentIdFieldName },
                        PageSize = 1024,
                        Start = start
                    }, null);
                    if (result.Results.Count == 0 || list.Count > 5000)
                        break;
                    start += result.Results.Count;
                    list.AddRange(result.Results.Select(x => x.Value<string>(Constants.DocumentIdFieldName)));
                }
                sp.Stop();

                Console.WriteLine("Read all ids {0:#,#} in {1:#,#} ms", list.Count, sp.ElapsedMilliseconds);

                var rand = new Random();
                list.Sort((s, s1) => rand.Next(-1, 1));
                sp.Restart();

                foreach (var id in list)
                {
                    store.DatabaseCommands.Get(id);
                }

                sp.Stop();
                Console.WriteLine("Read all docs {0:#,#} in {1:#,#} ms, avg: {2}", list.Count, sp.ElapsedMilliseconds, sp.ElapsedMilliseconds / (decimal)list.Count);
            }

            using (var store = new DocumentStore
                {
                    Url = "http://localhost:8080",
                    DefaultDatabase = dbName,
                }.Initialize())
            {
                var rand = new Random();
                list.Sort((s, s1) => rand.Next(-1, 1));
                var sp = Stopwatch.StartNew();

                foreach (var id in list)
                {
                    var a = store.DatabaseCommands.Get(id);
                }

                sp.Stop();
                Console.WriteLine("Read all docs {0:#,#} in {1:#,#} ms, avg: {2}", list.Count, sp.ElapsedMilliseconds, sp.ElapsedMilliseconds / (decimal)list.Count);    
            }
            
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
