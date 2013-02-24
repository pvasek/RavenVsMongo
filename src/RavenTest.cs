using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using Raven.Abstractions.Data;
using Raven.Client;
using Raven.Client.Document;
using Raven.Client.Indexes;
using RavenVsMongo.Entities;
using RavenVsMongo.Utils;

namespace RavenVsMongo
{
    public class RavenTest
    {
        public static TestResultSet Run(string databaseName, int readCount, int bulkSize, int generateCount)
        {
            var result = new TestResultSet();
            var generatedIds = new List<string>();
            var time = new Stopwatch();

            using (var documentStore = CreateDocumentStore(databaseName))
            {
                result.Write = GenerateItems(documentStore, bulkSize, generateCount, generatedIds);
            }

            using (var documentStore = CreateDocumentStore(databaseName))
            {
                result.CountItemsMs = ReadDocumentIds(documentStore, new List<string>());
            }

            using (var documentStore = CreateDocumentStore(databaseName))
            {
                result.Read1 = ReadItemsById(documentStore, readCount, generatedIds);
            }

            TestSettings.PauseBetweenReads();
            using (var documentStore = CreateDocumentStore(databaseName))
            {
                result.Read2 = ReadItemsById(documentStore, readCount, generatedIds);
            }

            TestSettings.PauseBetweenReads();
            using (var documentStore = CreateDocumentStore(databaseName))
            {
                result.Read3 = ReadItemsById(documentStore, readCount, generatedIds);
            }

            using (var documentStore = CreateDocumentStore(databaseName))
            {
                using (var session = documentStore.OpenSession())
                {
                    session.Advanced.MaxNumberOfRequestsPerSession = 5000;
                    for (var index = 0; index < TestSettings.NumberOfCategoriesTested; index++)
                    {
                        var category = "Category" + index;
                        time.Restart();
                        var list = session.Query<Person>().Where(i => i.CategoryId == category).Take(5000).ToList();
                        time.Stop();
                        result.Categories.Add(new TestResult { Count = list.Count, TotalMs = time.ElapsedMilliseconds });
                        Console.WriteLine("Category: {0}, Total time: {1}, readed#: {2}, avg time: {3}", category,
                                          time.ElapsedMilliseconds, list.Count, time.ElapsedMilliseconds / list.Count);
                    }
                }
            }

            using (var documentStore = CreateDocumentStore(databaseName))
            {
                using (var session = documentStore.OpenSession())
                {
                    time.Restart();
                    var itemCount = session.Query<Person>().Count();
                    time.Stop();
                    result.CountItemsMs = time.ElapsedMilliseconds;
                    Console.WriteLine("Count: {0}, Time: {1} ms", itemCount, time.ElapsedMilliseconds);
                    //CategoryCounts(session);
                }
            }

            using (var documentStore = CreateDocumentStore(databaseName))
            {
                Console.WriteLine("Deleting database...");
                documentStore.DatabaseCommands.ForSystemDatabase().Delete("Raven/Databases/" + databaseName, null);
                var dbDir = Path.Combine(ConfigurationManager.AppSettings["RavenDbPath"], databaseName);
                documentStore.Dispose();
                for (var i = 0; i < 1; i++)
                {                    
                    try
                    {
                        Directory.Delete(dbDir);
                        Console.WriteLine("Database deleted");
                        break;
                    }
                    catch (Exception)
                    {
                    }
                    Console.WriteLine("Could not delete database");
                    Thread.Sleep(5000);
                }
            }
            return result;
        }

        public static TestResultSet RunReadTest(string databaseName, int readCount)
        {
            var result = new TestResultSet();
            var ids = new List<string>();
            using (var documentStore = CreateDocumentStore(databaseName))
            {
                result.CountItemsMs = ReadDocumentIds(documentStore, ids);
            }
            
            using (var documentStore = CreateDocumentStore(databaseName))
            {
                result.Read1 = ReadItemsById(documentStore, readCount, ids);
            }

            TestSettings.PauseBetweenReads();
            using (var documentStore = CreateDocumentStore(databaseName))
            {
                result.Read2 = ReadItemsById(documentStore, readCount, ids);
            }

            TestSettings.PauseBetweenReads();
            using (var documentStore = CreateDocumentStore(databaseName))
            {
                result.Read3 = ReadItemsById(documentStore, readCount, ids);
            }
            return result;
        }

        private static long ReadDocumentIds(DocumentStore documentStore, List<string> ids)
        {
            int start = 0;
            var time = Stopwatch.StartNew();
            while (true)
            {
                var resultIds = documentStore.DatabaseCommands.Query("PersonList", new IndexQuery
                    {
                        FieldsToFetch = new[] { Constants.DocumentIdFieldName },
                        PageSize = 1024,
                        Start = start
                    }, null);
                if (resultIds.Results.Count == 0)
                    break;
                start += resultIds.Results.Count;
                ids.AddRange(resultIds.Results.Select(x => x.Value<string>(Constants.DocumentIdFieldName)));
            }
            time.Stop();
            Console.WriteLine("Ids count: {0}, time: {1}", ids.Count, time.ElapsedMilliseconds);
            return time.ElapsedMilliseconds;
        }

        private static TestResult ReadItemsById(DocumentStore documentStore, int readCount, List<string> generatedIds)
        {
            var ids = generatedIds.TakeRandom().ToList();
            var time = Stopwatch.StartNew();
            foreach (var id in ids)
            {
                documentStore.DatabaseCommands.Get(id);
            }
            time.Stop();
            var result = new TestResult { Count = readCount, TotalMs = time.ElapsedMilliseconds };
            Console.WriteLine("Reading total time: {0} ms, avg item: {1} ms, #records: {2}",
                              result.TotalMs, result.ItemAvgMs, ids.Count);
            return result;
        }

        private static DocumentStore CreateDocumentStore(string databaseName)
        {
            var documentStore = new DocumentStore
                {
                    Url = ConfigurationManager.AppSettings["RavenUrl"],
                    DefaultDatabase = databaseName
                };
            documentStore.JsonRequestFactory.ConfigureRequest += (sender, e) =>
                {
                    var httpWebRequest = ((System.Net.HttpWebRequest)e.Request);
                    httpWebRequest.UnsafeAuthenticatedConnectionSharing = true;
                    httpWebRequest.PreAuthenticate = true;
                    httpWebRequest.KeepAlive = true;
                };

            using (Profiler.Start("Initializing store"))
            {
                documentStore.Initialize();
            }
            using (Profiler.Start("Creating indexes"))
            {
                IndexCreation.CreateIndexes(typeof(Program).Assembly, documentStore);
            }
            return documentStore;
        }


        private static TestResult GenerateItems(IDocumentStore documentStore, int bulkSize, int generateCount, List<string> generatedIds)
        {
            var time = new Stopwatch();
            var writeResult = new TestResult();
            var totalRecords = 0;
            var generatingTotalTime = 0L;

            foreach (var chunkSize in ChunkUtils.GetChunks(generateCount, bulkSize))
            {
                //var items = Enumerable.Range(0, chunkSize).Select(PersonGenerator.Create).ToList();                    

                time.Restart();
                using (var bulkInsert = documentStore.BulkInsert())
                {
                    for (var i = 0; i < chunkSize; i++)
                    {
                        var item = PersonGenerator.Create();
                        bulkInsert.Store(item);
                        generatedIds.Add(item.Id);
                    }
                }
                time.Stop();
                generatingTotalTime += time.ElapsedMilliseconds;

                totalRecords += chunkSize;
                Console.WriteLine("Written {0} total: {1} records in: {2} ms", chunkSize, totalRecords,
                                  time.ElapsedMilliseconds);
            }
            writeResult.Count = generateCount;
            writeResult.TotalMs = generatingTotalTime;
            Console.WriteLine("Writing total time: {0} ms, avg item: {1} ms", generatingTotalTime, writeResult.ItemAvgMs);

            Console.WriteLine("Waiting for rebuilding indexes...");
            Thread.Sleep(TestSettings.WaitForRebuildIndexesMs);
            return writeResult;
        }
    }
}