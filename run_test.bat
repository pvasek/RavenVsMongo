net stop mongodb
net stop ravendb
net start mongodb
PING 1.1.1.1 -n 1 -w 60000 >NUL
PING 1.1.1.1 -n 1 -w 60000 >NUL
bin\RavenVsMongo.exe mongo_results.csv /testMode:Mongo /waitAfterGeneratingMs:60000 /itemsCounts:100,1000,10000

net stop mongodb
net stop ravendb
net start ravendb
PING 1.1.1.1 -n 1 -w 60000 >NUL
PING 1.1.1.1 -n 1 -w 60000 >NUL
bin\RavenVsMongo.exe raven_results.csv /testMode:Raven /waitAfterGeneratingMs:60000 /itemsCounts:100,1000,10000