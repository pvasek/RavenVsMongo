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
using RavenVsMongo.Indexes;
using RavenVsMongo.Utils;

namespace RavenVsMongo
{
    public class RavenTest
    {
        public static TestResultSet Run(string databaseName, int readCount, int? bulkSize = null, int? generateCount = null)
        {
            var result = new TestResultSet();
            
            var documentStore = new DocumentStore
                {
                    Url = ConfigurationManager.AppSettings["RavenUrl"], 
                    DefaultDatabase = databaseName
                };
            documentStore.JsonRequestFactory.ConfigureRequest += (sender, e) =>
            {
                var httpWebRequest = ((System.Net.HttpWebRequest) e.Request);
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

            var totalTime = Stopwatch.StartNew();
            var time = new Stopwatch();

            var generatedIds = new List<string>();
            #region generating
            if (generateCount != null && bulkSize != null)
            {                
                var totalRecords = 0;
                var generatingTotalTime = 0L;

                foreach (var chunkSize in ChunkUtils.GetChunks(generateCount.Value, bulkSize.Value))
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
                result.Write.Count = generateCount;
                result.Write.TotalMs = generatingTotalTime;
                Console.WriteLine("Writing total time: {0} ms, avg item: {1} ms", generatingTotalTime,
                                  generatingTotalTime / generateCount);

                Console.WriteLine("Waiting for rebuilding indexes...");
                Thread.Sleep(TestSettings.WaitForRebuildIndexesMs);
                
            }
            #endregion

            List<string> ids;
            using (var session = documentStore.OpenSession())
            {
                session.Advanced.MaxNumberOfRequestsPerSession = 100000;

                if (generatedIds.Count == 0)
                {
                    ids = Profiler.Measure("Reads ids", () =>
                                                            session.Query<Person>("PersonList")
                                                                   .Select(i => i.Id)
                                                                   .Take(readCount)
                                                                   .ToList());
                }
                else
                {
                    ids = generatedIds;
                }

                Console.WriteLine("Reading count: {0}, ids.count: {1}", readCount, ids.Count);
                
                ids = ids.TakeRandom().ToList();
                
                totalTime.Restart();
                foreach (var id in ids)
                {
                    var person = session.Load<Person>(id);
                    //var person = documentStore.DatabaseCommands.Get(id);
                    if (person == null)
                        throw new ArgumentException("Id doesn't exists");
                    //Console.WriteLine(person.LastName);
                }
                totalTime.Stop();
                result.Read.Count = readCount;
                result.Read.TotalMs = totalTime.ElapsedMilliseconds;
                Console.WriteLine("Reading total time: {0} ms, avg item: {1} ms, #records: {2}", totalTime.ElapsedMilliseconds, totalTime.ElapsedMilliseconds / readCount, ids.Count);
            }

            using (var session = documentStore.OpenSession())
            {
                session.Advanced.MaxNumberOfRequestsPerSession = 100000;
                totalTime.Restart();
                foreach (var id in ids)
                {
                    var person = session.Load<Person>(id);
                    //var person = documentStore.DatabaseCommands.Get(id);
                    if (person == null)
                        throw new ArgumentException("Id doesn't exists");
                    //Console.WriteLine(person.LastName);
                }
                totalTime.Stop();
                result.ReadRepeated.Count = readCount;
                result.ReadRepeated.TotalMs = totalTime.ElapsedMilliseconds;
                Console.WriteLine("Reading repeated total time: {0} ms, avg item: {1} ms, #records: {2}", totalTime.ElapsedMilliseconds, totalTime.ElapsedMilliseconds / readCount, ids.Count);
            }

            using (var session = documentStore.OpenSession())
            {
                session.Advanced.MaxNumberOfRequestsPerSession = 5000;
                for (var index = 0; index < TestSettings.NumberOfCategoriesTested; index++)
                {
                    var category = "Category" + index;
                    time.Restart();
                    var list = session.Query<Person>().Where(i => i.CategoryId == category).Take(5000).ToList();
                    time.Stop();
                    result.Categories.Add(new TestResult { Count = list.Count, TotalMs = time.ElapsedMilliseconds});
                    Console.WriteLine("Category: {0}, Total time: {1}, readed#: {2}, avg time: {3}", category, time.ElapsedMilliseconds, list.Count, time.ElapsedMilliseconds / list.Count);
                }
            }

            using (var session = documentStore.OpenSession())
            {
                time.Restart();
                var itemCount = session.Query<Person>().Count();
                time.Stop();
                result.CountItemsMs = time.ElapsedMilliseconds;
                Console.WriteLine("Count: {0}, Time: {1} ms", itemCount, time.ElapsedMilliseconds);                
                //CategoryCounts(session);
            }

            Console.WriteLine("Deleting database...");
            documentStore.DatabaseCommands.ForSystemDatabase().Delete("Raven/Databases/" + databaseName, null);
            var dbDir = Path.Combine(ConfigurationManager.AppSettings["RavenDbPath"], databaseName);
            documentStore.Dispose();
            for (var i = 0; i < 20; i++)
            {
                Thread.Sleep(5000);
                try
                {
                    Directory.Delete(dbDir);
                    Console.WriteLine("Database deleted");
                    break;
                }
                catch (Exception)
                {
                }
                Console.WriteLine("Could not database database");
            }

            return result;
        }

        private static void CategoryCounts(IDocumentSession session)
        {
            using (Profiler.Start("Person by category"))
            {
                var personByCategory = session
                    .Query<PersonCategoryCount, GroupByCategory>()
                    .Customize(i => i.WaitForNonStaleResultsAsOfNow())
                    .ToList();
                foreach (var source in personByCategory.OrderBy(i => i.CategoryId))
                {
                    Console.WriteLine("{0}: {1}", source.CategoryId, source.Count);
                }
            }
        }
    }
}