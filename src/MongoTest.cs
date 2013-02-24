using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Driver;
using RavenVsMongo.Entities;
using RavenVsMongo.Utils;

namespace RavenVsMongo
{
    public class MongoTest
    {
        static MongoTest()
        {
            BsonSerializer.RegisterIdGenerator(typeof(string), StringObjectIdGenerator.Instance);
            var map = new BsonClassMap<Person>();
            map.AutoMap();
            BsonClassMap.RegisterClassMap(map);
        }

        public static TestResultSet Run(string databaseName, int readCount, int? bulkSize = null, int? generateCount = null)
        {
            var result = new TestResultSet();

            Console.WriteLine("Initializing store...");

            var client = new MongoClient("mongodb://127.0.0.1/?safe=true");
            var db = client.GetServer().GetDatabase(databaseName);
            
            Console.WriteLine("Initialized.");

            var collection = db.GetCollection<Person>("Person");
            collection.EnsureIndex("CategoryId");


            var totalTime = Stopwatch.StartNew();
            var time = new Stopwatch();
            time.Restart();

            var generatedIds = new List<string>();
            if (generateCount != null && bulkSize != null)
            {               
                var totalRecords = 0;
                var generatingTotalTime = 0L;
                foreach (var chunkSize in ChunkUtils.GetChunks(generateCount.Value, bulkSize.Value))
                {
                    time.Restart();
                    for (var i = 0; i < chunkSize; i++)
                    {
                        var item = PersonGenerator.Create();
                        collection.Save(item);
                        generatedIds.Add(item.Id);
                    }
                    time.Stop();
                    generatingTotalTime += time.ElapsedMilliseconds;
                    totalRecords += chunkSize;

                    Console.WriteLine("Written {0} total: {1} records in: {2} ms", chunkSize, totalRecords,
                                      time.ElapsedMilliseconds);
                }
                result.Write.Count = generateCount;
                result.Write.TotalMs = generatingTotalTime;
                Console.WriteLine("Writing total time: {0} ms, avg item: {1} ms", generatingTotalTime, generatingTotalTime / generateCount);

                Console.Write("Waiting for rebuilding indexes...");
                Thread.Sleep(TestSettings.WaitForRebuildIndexesMs);
            }

            List<string> ids;
            if (generatedIds.Count == 0)
            {
                ids = Profiler.Measure("Reads ids", () =>
                                                    collection
                                                        .FindAll()
                                                        .AsQueryable()
                                                        .Take(readCount)
                                                        .Select(i => i.Id)
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
                var person = collection.FindOneById(id);
                if (person == null)
                    throw new ArgumentException("Id doesn't exist.");
                //Console.WriteLine(person.LastName);
            }
            totalTime.Stop();
            result.Read.Count = readCount;
            result.Read.TotalMs = totalTime.ElapsedMilliseconds;
            Console.WriteLine("Reading total time: {0} ms avg: {1} ms for #records: {2}", totalTime.ElapsedMilliseconds, totalTime.ElapsedMilliseconds / (decimal)ids.Count, ids.Count);

            var collection2 = db.GetCollection<Person>("Person");
            totalTime.Restart();
            foreach (var id in ids)
            {
                var person = collection2.FindOneById(id);
                if (person == null)
                    throw new ArgumentException("Id doesn't exist.");
                //Console.WriteLine(person.LastName);
            }
            totalTime.Stop();
            result.ReadRepeated.Count = readCount;
            result.ReadRepeated.TotalMs = totalTime.ElapsedMilliseconds;
            Console.WriteLine("Reading repeated total time: {0} ms avg: {1} ms for #records: {2}", totalTime.ElapsedMilliseconds, totalTime.ElapsedMilliseconds / (decimal)ids.Count, ids.Count);


            for (var index = 0; index < TestSettings.NumberOfCategoriesTested; index++)
            {
                var category = "Category" + index;
                time.Restart();
                var list = collection.FindAll().AsQueryable().Where(i => i.CategoryId == category).ToList();//.Find(Query.EQ("CategoryId", category)).ToList();
                time.Stop();
                Console.WriteLine("Category: {0}, Total time: {1}, readed#: {2}, avg time: {3}", category, time.ElapsedMilliseconds, list.Count,
                    list.Count == 0 ? (object) null : time.ElapsedMilliseconds / list.Count);
                result.Categories.Add(new TestResult{ Count = list.Count, TotalMs = time.ElapsedMilliseconds});
            }

            client.GetServer().DropDatabase(databaseName);

            return result;
        } 
    }
}