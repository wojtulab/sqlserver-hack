# sqlserver-hack
Simple debug connection and recover access to MsSQL server using psexec (system account) or SQLwriter.

# INFO
Script produce txt file (fullscript.txt + .bat) with prepared commands to recover access to specified SQL server instnace. You can use additional debug functions to discover SQLservices and get details: license, is clustered, is alwayson enabled, listening ports etc.

# SETUP
Ccopy all files to any directory (portable)

# RUN
Open START file then it will open with admin rights hack.bat script.

# KNOWN BUGS
+ Clusters discovery (listing more details)

# EXAMPLE
Genereted fullscript.txt file content:
```::PSEXEC method: 
"C:\WK_Scripts\_Projects\LostPassword\slim\bin\psexec.exe" -accepteula -i -s -d sqlcmd.exe -S SQL2019s.contoso.com -E -i C:\WK_Scripts\_Projects\LostPassword\slim\bin\psexec.sql 
------------------------------- 
::sqlcmd method: 
REG EXPORT HKLM\SYSTEM\CurrentControlSet\Services\SQLWriter C:\temp\sql.reg /y 
::START 
reg add HKLM\SYSTEM\CurrentControlSet\Services\SQLWriter /v ImagePath /d """"""" -S SQL2019s.contoso.com -E -Q """CREATE LOGIN [contoso\wk] from windows; ALTER SERVER ROLE sysadmin ADD MEMBER [contoso\wk];"" /f 
net stop SQLWriter 
net start SQLWriter 
reg add HKLM\SYSTEM\CurrentControlSet\Services\SQLWriter /v ImagePath /d "C:\Program Files\Microsoft SQL Server\90\Shared\sqlwriter.exe" /f 
net start SQLWriter 
::STOP 
REG IMPORT C:\temp\sql.reg 
::TEST: 
sqlcmd.exe -S SQL2019s.contoso.com -h -1 -E -i C:\WK_Scripts\_Projects\LostPassword\slim\bin\chck.sql 
```
# screens
![screen1: scan for sql](https://github.com/wojtulab/sqlserver-hack/blob/master/screen.jpg)

# changelog 
+ -=version 1.0=-
+ added:
+ scan for browser;
+ scan for sqlservr.exe;
+ scan for open ports;
+ scan for registry entries;
+ scan for multiple sql instances;

++ -=version 1.1=-
+ added:
+ scan for WMI;
+ scan for Windows Registry;
+ scan for protocols: tcp, name pipe and local;
+ debug info regarding sql clustering;
+ MS Clustering scan (cluster name, nodes)
+ cluster mssql discovery;
+ AlwaysOn discovery;
+ scan network for SQLservices;
+ variables moved to external file;
+ recovery access script saved to txt and bat formats;
+ management studio with system account (login to sql without pass);
+ vmware detection
