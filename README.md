# sqlserver-hack
Recover access to MsSQL server

# INFO
Script produce txt file (fullscript.txt) with prepared commands to recover access to specified SQL server instnace.

# SETUP
Just copy all files to any directory

# RUN
Open START file then it will open with admin rights hack.bat script.

# KNOWN BUGS
--Clusters

# EXAMPLE
Genereted fullscript.txt file content:
```PSEXEC method: 
"C:\WK_Scripts\_Projects\LostPassword\slim\bin\psexec.exe" -accepteula -i -s -d sqlcmd.exe -S SQL2019s.contoso.com -E -i C:\WK_Scripts\_Projects\LostPassword\slim\bin\psexec.sql 
------------------------------- 
sqlcmd method: 
REG EXPORT HKLM\SYSTEM\CurrentControlSet\Services\SQLWriter C:\temp\sql.reg /y 
----START 
reg add HKLM\SYSTEM\CurrentControlSet\Services\SQLWriter /v ImagePath /d """"""" -S SQL2019s.contoso.com -E -Q """CREATE LOGIN [contoso\wk] from windows; ALTER SERVER ROLE sysadmin ADD MEMBER [contoso\wk];"" /f 
net stop SQLWriter 
net start SQLWriter 
reg add HKLM\SYSTEM\CurrentControlSet\Services\SQLWriter /v ImagePath /d "C:\Program Files\Microsoft SQL Server\90\Shared\sqlwriter.exe" /f 
net start SQLWriter 
----STOP 
REG IMPORT C:\temp\sql.reg 
----TEST: 
to test use: 
sqlcmd.exe -S SQL2019s.contoso.com -h -1 -E -i C:\WK_Scripts\_Projects\LostPassword\slim\bin\chck.sql 
```