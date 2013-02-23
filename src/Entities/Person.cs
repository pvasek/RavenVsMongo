using RavenDbTest.Console.Entities;

namespace RavenVsMongo.Entities
{
    public class Person
    {
        public string Id { get; set; }
        public string CategoryId { get; set; }
        public GenderType Gender { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string DataLoad { get; set; }
    }
}