using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using RavenDbTest.Console.Entities;
using RavenVsMongo.Entities;
using ServiceStack.Text;

namespace RavenVsMongo.Utils
{
    public static class PersonGenerator
    {
        public static int ItemSizeKb = 100;

        static PersonGenerator()
        {
            _names = File.ReadAllText("names.json").FromJson<Names>();
            var serializer = new JsonSerializer<Person>();
            _emptySize = serializer.SerializeToString(new Person()).Length;
        }

        private static readonly Random _random = new Random();
        private static readonly Names _names;
        private static readonly int _emptySize;

        public static Person Create()
        {
            var result = new Person();
            result.Id = null;
            result.CategoryId = "Category" + _random.Next(10).ToString();
            result.Gender = (GenderType) _random.Next(1);
            var fullName = _names.GetName(_random, result.Gender);
            result.FirstName = fullName.FirstName;
            result.LastName = fullName.LastName;
            result.DataLoad = GenerateText(ItemSizeKb);
            return result;
        }

        public static string GenerateText(int sizeKb)
        {
            var sizeBytes = sizeKb*1024 - _emptySize;
            return "".PadRight(sizeBytes, 'a');
        }
    }
}