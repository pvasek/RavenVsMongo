Raven - Mongo (basic performance comparison)
============
This is a simple test comparing basic write/read performance of RavenDB and MongoDB in connection to the document size.
There is just one entity/document Person. This includes property mostly for making document big enough. Except the CotagoryId property which is used also for filtering.
There is 1000 person generated in 10 categories so it is quite small data set. The generated data try to be more realistic therefore the document size is just estimate that can differ a little bit.

Read Items by ID
----------------
Selects 500 ids and read them with random order from DB. 

Raven: `Load<Person>(id)`

Mongo: `FindOneById(id)`

Read Items – filtered
---------------------
Filters Person by category for first 5 categories 

Raven: `Query<Person>().Where(i => i.CategoryId == categoryId).ToList()`

Mongo: `AsQueryable().Where(i => i.CategoryId == categoryId).ToList()`

Write Items
-----------
Writes 1000 generated items to the DB. Raven test use bulk insert feature!

Raven: `BulkInsert.Store(item)`

Mongo: `Save(item)`
