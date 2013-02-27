cd bin

net stop mongodb
net stop ravendb
net start ravendb
PING 1.1.1.1 -n 1 -w 60000 >NUL
PING 1.1.1.1 -n 1 -w 60000 >NUL
RavenVsMongo.exe test raven_results.csv /testMode:Raven /waitAfterGeneratingMs:180000 /itemsCounts:100000 /documentSizes:1,100,250

cd ..