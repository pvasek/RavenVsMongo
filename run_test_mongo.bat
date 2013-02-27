cd bin

net stop mongodb
net stop ravendb
net start mongodb
PING 1.1.1.1 -n 1 -w 60000 >NUL
PING 1.1.1.1 -n 1 -w 60000 >NUL
RavenVsMongo.exe test mongo_results.csv /testMode:Mongo /waitAfterGeneratingMs:60000 /itemsCounts:100,1000,10000

cd ..