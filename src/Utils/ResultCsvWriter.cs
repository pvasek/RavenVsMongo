using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace RavenVsMongo.Utils
{
    public static class ResultCsvWriter
    {
        public static void WriteToFile(string fileName, List<Result> results)
        {
            using (var sw = new StreamWriter(fileName, false, Encoding.ASCII))
            {
                Write(sw, results);    
            }
        }

        private static void Write(TextWriter output, List<Result> list)
        {
            WriteHeader(output);
            WriteRows(output, list);
        }

        public static void WriteRows(TextWriter output, List<Result> list)
        {
            foreach (var result in list)
            {
                if (result.Raven != null)
                {
                    WriteRow(output, result.ItemsCount, result.DocumentSizeKb, result.Raven, "Raven");
                }
                if (result.Mongo != null)
                {
                    WriteRow(output, result.ItemsCount, result.DocumentSizeKb, result.Mongo, "Mongo");
                }
            }
        }

        public static void WriteHeader(TextWriter output)
        {
            var cols = new List<object>();
            cols.Add("Type");
            cols.Add("Items Count");
            cols.Add("Document Size [kB]");
            cols.Add("Read item avg [ms]");
            cols.Add("Read repeated item avg [ms]");
            cols.Add("Read filtered item avg [ms]");
            cols.Add("Write item avg [ms]");
            cols.Add("Read count [ms]");

            output.WriteLine(String.Join(",", cols));
        }

        private static void WriteRow(TextWriter output, int itemsCount, int documentSize, TestResultSet result, string name)
        {
            var cols = new List<object>();
            cols.Add(name);
            cols.Add(itemsCount);
            cols.Add(documentSize);
            cols.Add(result.Read.ItemAvgMs);
            cols.Add(result.ReadRepeated.ItemAvgMs);
            cols.Add(result.Categories.Average(i => i.ItemAvgMs));
            cols.Add(result.Write.ItemAvgMs);
            cols.Add(result.CountItemsMs);

            output.WriteLine(String.Join(",", cols));
        }
    }
}