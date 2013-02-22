using System.Linq;
using Raven.Client.Indexes;
using RavenVsMongo.Entities;

namespace RavenVsMongo.Indexes
{
    public class PersonCategoryCount
    {
        public string CategoryId { get; set; }
        public int Count { get; set; }
    }

    public class GroupByCategory : AbstractIndexCreationTask<Person, PersonCategoryCount>
    {
        public GroupByCategory()
        {
            Map = people => people
                .Select(i => new PersonCategoryCount
                    {
                        CategoryId = i.CategoryId,
                        Count = 1
                    });

            Reduce = results => results
                .GroupBy(i => i.CategoryId)
                .Select(g => new PersonCategoryCount 
                    {
                        CategoryId = g.Key,
                        Count = g.Sum(i => i.Count)
                    });
        }
    }
}