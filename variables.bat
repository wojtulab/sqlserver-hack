@ECHO OFF
::variables source
::main vars
fc "user.txt" "blank.txt" > nul && ( FOR /F "tokens=* USEBACKQ" %%F IN (`whoami`) DO ( SET who=%%F ) ) || ( set /p who=<user.txt )
set who=%who: =%
set host=%COMPUTERNAME%
set domain=%USERDOMAIN%
set sc=fullscript.txt
set tst=test-if-iam-sysadmin.bat
set sfound="mssql_founded.txt"
::others
set _major=0
set many=0
echo. > %sfound%
cluster group 2>nul && set _cluerr=0 || set _cluerr=1
sc query |findstr .SQLB. >nul && Echo.SQLBrowser is running. && set _browser=1 || Echo.SQLBrowser is disabled. && set _browser=0
