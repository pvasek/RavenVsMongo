using System;
using System.Collections.Generic;
using RavenDbTest.Console.Entities;

namespace RavenVsMongo.Utils
{
    public class FullName
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
    }

    public class Names
    {
        public List<string> MenFirstNames { get; set; }
        public List<string> MenLastNames { get; set; }
        public List<string> WomenFirstNames { get; set; }
        public List<string> WomenLastNames { get; set; }

        public FullName GetName(Random random, GenderType gender)
        {
            var firstNames = gender == GenderType.Men ? MenFirstNames : WomenFirstNames;
            var lastNames = gender == GenderType.Men ? MenLastNames : WomenLastNames;

            return new FullName
                {
                    FirstName = firstNames[random.Next(firstNames.Count)],
                    LastName = lastNames[random.Next(lastNames.Count)]
                };
        }
    }
}