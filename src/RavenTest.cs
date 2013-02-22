using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        public static TestResultSet Run(int readCount, int? bulkSize = null, int? generateCount = null)
        {
            var result = new TestResultSet();

            var documentStore = new DocumentStore { ConnectionStringName = "Raven"};
            documentStore.JsonRequestFactory.ConfigureRequest += (sender, e) =>
            {
                ((System.Net.HttpWebRequest)e.Request).UnsafeAuthenticatedConnectionSharing = true;
                e.Request.PreAuthenticate = true;
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
                using (Profiler.Start("Delete items"))
                {
                    documentStore.DatabaseCommands.DeleteByIndex(typeof (PersonList).Name, new IndexQuery(), true);
                }

                var totalRecords = 0;
                var generatingTotalTime = 0L;

                foreach (var chunkSize in ChunkUtils.GetChunks(generateCount.Value, bulkSize.Value))
                {
                    //var items = Enumerable.Range(0, chunkSize).Select(PersonGenerator.Create).ToList();                    

                    time.Restart();
                    using (var bulkInsert = documentStore.BulkInsert("test"))
                    {
                        for (var i = 0; i < chunkSize; i++)
                        {
                            var item = PersonGenerator.Create(i);
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

            using (var session = documentStore.OpenSession())
            {
                session.Advanced.MaxNumberOfRequestsPerSession = 100000;

                List<string> ids;
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
                    //var person = session.Load<Person>(id);
                    var person = documentStore.DatabaseCommands.Get(id);
                    if (person == null)
                        throw new ArgumentException("Id doesn't exists");
                    //Console.WriteLine(person.LastName);
                }
                totalTime.Stop();
                result.Read.Count = readCount;
                result.Read.TotalMs = totalTime.ElapsedMilliseconds;
                Console.WriteLine("Total time: {0} ms, avg item: {1} ms, #records: {2}", totalTime.ElapsedMilliseconds, totalTime.ElapsedMilliseconds / readCount, ids.Count);
            }

            using (var session = documentStore.OpenSession())
            {
                session.Advanced.MaxNumberOfRequestsPerSession = 5000;
                for (var index = 0; index < 5; index++)
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

            if (generateCount != null && bulkSize != null)
            {
                using (Profiler.Start("Delete items"))
                {
                    documentStore.DatabaseCommands.DeleteByIndex(typeof(PersonList).Name, new IndexQuery(), true);
                }
                result.DeletingItemsMs = Profiler.LastTimeMs;
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