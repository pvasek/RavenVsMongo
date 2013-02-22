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
        public static int TextPropertyWordCount = 100;

        static PersonGenerator()
        {
            _names = File.ReadAllText("names.json").FromJson<Names>();
        }

        private static readonly string[] _words = @"the be to of and in that have it for not on with he as you do at this but his by from they we say her she or an will my one all would there their what so up out if about who get which go me when make can like time no just him know take people into year your good some could them see other than then now look only come its over think also back after use two how our work first well way even new want because any these give day most us"
            .Split(' ');

        private static readonly Random _random = new Random();
        private static readonly PropertyInfo[] _properties = typeof(Person).GetProperties(BindingFlags.Public | BindingFlags.Instance);
        private static readonly Names _names;

        public static Person Create(int counter)
        {
            var result = new Person();
            foreach (var prop in _properties.Where(i => i.PropertyType == typeof(string)))
            {
                prop.SetValue(result, GetRandomText(_random, TextPropertyWordCount), null);
            }
            result.Id = null;
            result.CategoryId = "Category" + _random.Next(10).ToString();
            result.Gender = (GenderType) _random.Next(1);
            var fullName = _names.GetName(_random, result.Gender);
            result.FirstName = fullName.FirstName;
            result.LastName = fullName.LastName;
            return result;
        }

        public static string GetRandomText(Random random, int wordCount)
        {
            if (wordCount == 0)
                return "";

            var sb = new StringBuilder();
            var actualWordCount = 0;
            while (actualWordCount < wordCount)
            {
                sb.Append(" ");
                sb.Append(_words[random.Next(_words.Count())]);
                actualWordCount++;
            }
            sb.Remove(0, 1);
            return sb.ToString();
        }
    }
}