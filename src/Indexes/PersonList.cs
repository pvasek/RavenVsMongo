using System.Linq;
using Raven.Client.Indexes;
using RavenVsMongo.Entities;

namespace RavenVsMongo.Indexes
{
    public class PersonList : AbstractIndexCreationTask<Person>
    {
        public PersonList()
        {
            Map = people => from person in people 
                            select new
                                {
                                    person.Id,
                                    person.LastName,
                                    person.FirstName
                                };
        }
    }
}