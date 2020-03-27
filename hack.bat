@ECHO OFF
:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
::: 				   WK				                :::
::: 	Last update: 2020-03-20     v1.1                :::
:::                                                     :::
::: please contact: kucyk87@gmail.com         			:::
:::                        WK                           :::
:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
cd %cd%
IF "%cd%"=="C:\WINDOWS\system32" echo wrong path, run this script from cmd.exe as admin (just open cmd and call this script) & goto exitting
set host=%COMPUTERNAME%
set domain=%USERDOMAIN%
fc "user.txt" "blank.txt" > nul && ( FOR /F "tokens=* USEBACKQ" %%F IN (`whoami`) DO ( SET who=%%F ) ) || ( set /p who=<user.txt )
set who=%who: =%
echo.account %who% will be used (specify user inside user.txt)
sc query |findstr .SQLB. >nul && Echo.SQLBrowser is running. && set _browser=1 || Echo.SQLBrowser is disabled. && set _browser=0
set sc=fullscript.txt
set tst=test-if-iam-sysadmin.bat
set sfound="mssql_founded.txt"
echo. > %sfound%
::echo is current path correct?
::echo %cd%
::echo.
::echo and running with admin rights? (ctrl+c to cancel)
::PAUSE
::cls
echo.
echo --------------------------------------------
echo domain: %domain%
echo current hostname: %COMPUTERNAME%
for /f "tokens=1-2*" %%A in ('net statistics workstation ^| find "Statistics since"') do echo uptime: %%C
echo --------------------------------------------
:Starting
echo.1) generate scripts
echo.2) continue discovery SQLservices
set menu=
choice /c 123 /n /m "Choose a task"
set menu=%errorlevel%
if errorlevel 1 set goto=fullscript
if errorlevel 2 set goto=Scan
if errorlevel 3 set goto=quit
cls
goto %goto%

:Scan
FOR /F "tokens=* USEBACKQ" %%F IN (`tasklist ^| findstr sqlservr.exe ^| find /c /v ""`) DO ( SET sqlcount=%%F )
FOR /F "tokens=* USEBACKQ" %%F IN (`sc query ^|findstr ClusSv. ^| find /c /v ""`) DO ( SET sqlclust=%%F )
tasklist | findstr sqlservr.exe >nul && echo.SQL proccess sqlservr.exe is running. (%sqlcount% proces(s)) && set _sqlserv=1 || echo sqlservr.exe process is not running && set _sqlserv=0
::IF %sqlcount% gtr 1 ( echo INFO: more than one instance is running. )
::call sqlcmd -r1 -Q"" -S tcp:%%a\%%a -l 1 2>nul && echo %%a\%%a
::echo %%a\%%a && call sqlcmd -r1 -Q"" -S tcp:%%a\%%a -l 1 2>nul && echo %%a\%%a
cluster group 2>nul && set _cluerr=0 || set _cluerr=1
IF %sqlclust% geq 1 ( 
echo.-------------------------
echo.INFO: one or more instance could be CLUSTERED. 
IF %_cluerr% neq 1 (
echo.Founded SQL server clusterd instances:
for /f "tokens=4 delims= " %%a in ('cluster res ^| findstr "SQL" ^| findstr Server ^| findstr /v Agent') do (
echo %%a\%%a >> %sfound% 
call sqlcmd -l1 -Q"print'connected successfully:';" -S tcp:%%a\%%a 2>&1 |findstr /c:"connected" /c:"Login failed" && echo %%a\%%a
)
)
IF %_cluerr% equ 1 (
echo.Cluster ressources:
call powershell -command "Get-ClusterResource | where { $_.ResourceType -like '*sql*' }" 2> nul
call powershell -command "if ($($(Get-ClusterResource | where { $_.ResourceType -like '*sql*' }).ResourceType).Name -like '*Availability*' ) { write-host 'INFO: AlwaysOn detected' }" 2> nul
)
)
echo.---------------
if %_sqlserv% equ 1 (
echo.
echo.[1/4] Running SQL Services scan: && sc query | findstr /R [a-z]*MSSQLSERVER | findstr SERVICE | findstr /V Launcher >nul && ( sc query | findstr /R [a-z]*MSSQLSERVER | findstr SERVICE | findstr /V Launcher )
call sqlcmd -l1 -Q"print'connected successfully:';" -S tcp:%COMPUTERNAME% 2>&1 |findstr /c:"connected" /c:"Login failed" && echo %COMPUTERNAME% && echo %COMPUTERNAME% >> %sfound% 
sc query | findstr .MSSQL$. | findstr SERVICE | findstr /V Launcher >nul && ( sc query | findstr .MSSQL$. | findstr SERVICE | findstr /V Launcher )
for /f "tokens=2 delims=$" %%a in ('sc query ^| findstr .MSSQL$. ^| findstr SERVICE ^| findstr /V Launcher ') do ( 
call sqlcmd -l1 -Q"print'connected successfully:';" -S tcp:%COMPUTERNAME%\%%a 2>&1 |findstr /c:"connected" /c:"Login failed" && echo %COMPUTERNAME%\%%a && echo %COMPUTERNAME%\%%a >> %sfound% 
)
echo ------------------------------
echo.[2/4] registry scan [if hives exists]
( for /f "tokens=2*" %%a in ('REG QUERY HKLM\SYSTEM\CurrentControlSet\Services\MSSQLServer /v DisplayName') do echo service name: %%b ) 2> nul && ( for /f "tokens=2*" %%a in ('REG QUERY HKLM\SYSTEM\CurrentControlSet\Services\MSSQLServer /v DisplayName') do echo service name: %%b )
( for /f "tokens=2*" %%a in ('REG QUERY HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\MSSQLServer\Setup /v SqlPath') do echo.Path: %%b ) 2> nul && ( for /f "tokens=2*" %%a in ('REG QUERY HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\MSSQLServer\Setup /v SqlPath') do echo.Path: %%b )
( for /f "tokens=2*" %%a in ('REG QUERY HKLM\SYSTEM\CurrentControlSet\Services\MSSQLServer /v ImagePath') do  echo.Bin: %%b ) 2> nul && ( for /f "tokens=2*" %%a in ('REG QUERY HKLM\SYSTEM\CurrentControlSet\Services\MSSQLServer /v ImagePath') do  echo.Bin: %%b ) || echo registry hives is not detected. Skipping.
echo.
echo ------------------------------
echo [3/4] WMI scan...
call cscript /Nologo discovery.vbs 2> nul || echo SQL server is not detected. Skipping
echo ------------------------------
echo.[4/4]SQLserver scan ports:
for /f "tokens=2*" %%a in ('tasklist /svc ^| findstr sqlser') do ( netstat -ano | findstr %%a | findstr LIST. )
) ELSE ( echo.sqlservr.exe process is not running )

echo ==========================================
:Menu
echo.1) generate scripts
echo.2) use browser to scan local instances
echo.3) use full WMI scan local instances (with sql 'MAJOR VERSION' from above)
echo.4) use sqlcmd to scan all protocols (tcp, name pipe and local)
echo.5) exit
set menu=
choice /c 12345 /n /m "Choose a task"
set menu=%errorlevel%
if errorlevel 1 set goto=fullscript
if errorlevel 2 set goto=browser
if errorlevel 3 set goto=fullwmi
if errorlevel 4 set goto=Protocols
if errorlevel 5 set goto=quit
cls
goto %goto%

:Protocols
set /p sqlt=please type connection string/instance name:
echo sqlcmd -S tcp:%sqlt% (tcp connection)
( call sqlcmd -r1 -l 1 -h -1 -S tcp:sqlcmd -S tcp:%sqlt% -Q"" 2>nul ) && echo.ok || echo.KO
echo --
echo sqlcmd -S np:%sqlt% (name pipe connection)
( call sqlcmd -r1 -l 1 -h -1 -S np:%sqlt% -Q"" 2>nul ) && echo.ok || echo.KO
echo --
echo sqlcmd -S lpc:%sqlt% (local connection)
( call sqlcmd -r1 -l 1 -h -1 -S lpc:%sqlt% -Q"" 2>nul ) && echo.ok || echo.KO
goto Menu

:quit
exit

:fullwmi
call cscript /Nologo discovery.vbs 2> nul || echo SQL server is not detected. Skipping
set /p versionwmi=SQL version (major):
IF [%versionwmi%] == [] echo.empty version, skipping && goto Menu
call cscript /Nologo discover_full.vbs %versionwmi%
goto Menu

:fullscript
setlocal enabledelayedexpansion
    ::incremental varibale
    set "i=0"
    ::store filenames into array
    ::echo Default MsSQL instances:
	for /f "tokens=*" %%f in ('sc query ^| findstr /R [a-z]*MSSQLSERVER ^| findstr SERVICE ^| findstr /V Launcher') do (
      set arr[!i!]=%%f & set /a "i+=1"
    )
    ::display all array items
	set "len=!i!"
	if %len% equ 0 goto:wynikdef
    set "i=0"
    :loop
    set /a "i+=1"
	if %i% neq %len% goto:loop
	:wynikdef
	echo Default MsSQL instances: !i!
  endlocal
  ::another way to create array arr.!i!=%%f
::NAMED SQL SERVICES:
  setlocal enabledelayedexpansion
    ::incremental varibale
    set "i=0"
    ::store filenames into array
	echo.
	::echo Named MsSQL instances:
    for /f "tokens=*" %%f in ('sc query ^| findstr .MSSQL$. ^| findstr SERVICE ^| findstr /V Launcher') do (
      set arr[!i!]=%%f & set /a "i+=1"
    )
    ::display all array items
    set "len=!i!"
    if %len% equ 0 goto:wyniknam
	set "i=0"
    :loop2
    SET snamed=!arr[%i%]!
	SET snamed=%snamed:MSSQL$=\ %
	SET snamed=%snamed:SERVICE_NAME:= %
	set snamed=%snamed: =%
	call sqlcmd -l1 -Q"print'connected successfully:';" -S tcp:%COMPUTERNAME%%snamed% 2>&1 |findstr /c:"connected" /c:"Login failed" && echo %COMPUTERNAME%%snamed%
	set /a "i+=1"
	if %i% neq %len% goto:loop2
	:wyniknam
	 echo Named MsSQL instances: !i!
  Endlocal&( set "sqlc=%COMPUTERNAME%%snamed%"
	)
  goto ChooseSQL

:Named
::Echo.MsSQL named instance founded:
::FOR /F "tokens=* USEBACKQ" %%F IN (`sc query ^| findstr .MSSQL$. ^| findstr SERVICE ^| findstr /V Launcher`) DO ( SET snamed=%%F )
::SET snamed=%snamed:MSSQL$=\ %
::SET snamed=%snamed:SERVICE_NAME:= %
::set snamed=%snamed: =%
::set snamed=%COMPUTERNAME%%snamed%
::ECHO %snamed%
::set sqlc=%snamed%
::goto ChooseSQL

:Default
::Echo.MsSQL default instance founded:
::set sqlc=%COMPUTERNAME%
::echo %sqlc%
::sc query | findstr .MSSQL$. | findstr SERVICE | findstr /V Launcher >nul && (echo ====== && echo !INFO!: other SQLs founded: && echo HOST: %COMPUTERNAME% && sc query | findstr .MSSQL$. | findstr SERVICE | findstr /V Launcher && echo ====== )
::goto ChooseSQL

:ChooseSQL
echo.
::echo.Protocols check:
::echo sqlcmd -S np:%sqlc%
::( call sqlcmd -h -1 -S np:%sqlc% -Q "set nocount on; print 'connected';" 2>nul ) && echo.Name Pipe is ok || echo.Name Pipe not ok
::echo --
::echo sqlcmd -S lpc:%sqlc%
::( call sqlcmd -h -1 -S lpc:%sqlc% -Q "set nocount on; print 'connected';" 2>nul ) && echo.Local protocol is ok || echo.Local protocol not ok
::echo.
echo.
echo. Founded MsSQL in discovery:
type %sfound%
echo.
echo checking connection to founded SQL instances:
for /F "usebackq tokens=*" %%A in (%sfound%) do (
echo %%A connecting...
call sqlcmd -l1 -Q"print'connected successfully:';" -S tcp:%%A 2>&1 |findstr /c:"connected" /c:"Login failed"
)
echo.
echo ------------------------------
echo.account %who% will be used
echo ------------------------------
echo.1) use default: %COMPUTERNAME%
echo.2) use named sql: %sqlc%
echo.3) use another instance...
echo.4) use all instances from discovery

set menu=
choice /c 1234 /n /m "Choose a task"
set many=0
set menu=%errorlevel%
if errorlevel 1 set goto=Nextstuff1
if errorlevel 2 set goto=Nextstuff2
if errorlevel 3 set goto=Specified
if errorlevel 4 ( set goto=Nextstuff 
set many=1 
)
goto %goto%

:browser
cls
echo scanning Microsoft SQLBROWSER for sql services...
if %_browser% equ 1 (call PortQry.exe -n localhost -p udp -o 1434 | findstr "ServerName InstanceName tcp Version IsClustered") ELSE (echo.SQLBrowser is disabled. Starting... && ( net start SQLBrowser 2> nul && call PortQry.exe -n localhost -p udp -o 1434 ) )
goto Menu

:Specified
set /p sqlc=please type connection string/instance name:
goto Nextstuff

:Nextstuff1
set sqlc=%COMPUTERNAME%
echo using %sqlc%
goto Nextstuff

:Nextstuff2
echo using %sqlc%
goto Nextstuff

:Nextstuff
cls
if %_browser% equ 1 (
echo ----------------REMEMBER to use: -------------------------
echo.
echo current version:
call PortQry.exe -n localhost -p udp -o 1434 2>nul | findstr "Version"
echo.
echo "<= 10.5 version -> using psexec method from generated script"
echo ">= 11.0 version -> using writer method from generated script"
echo.
echo ----------------REMEMBER ----------------------------------
)
echo "for <= 10.5 version -> use PSEXEC method:" > %sc%
IF %many% equ 1 (
for /F "usebackq tokens=*" %%A in (%sfound%) do (
echo "%cd%\psexec.exe" -accepteula -i -s -d sqlcmd.exe -S %%A -E -i %cd%\psexec.sql >> %sc%)
) else (
echo "%cd%\psexec.exe" -accepteula -i -s -d sqlcmd.exe -S %sqlc% -E -i %cd%\psexec.sql >> %sc%
)
FOR /F "tokens=* USEBACKQ" %%F IN (`where SQLCMD.exe`) DO ( SET scmd=%%F )
::set query=CREATE LOGIN [%who%] from windows; ALTER SERVER ROLE sysadmin ADD MEMBER [%who%];
set query=CREATE LOGIN [%who%] from windows; exec sp_addsrvrolemember '%who%', 'sysadmin';
set regp=HKLM\SYSTEM\CurrentControlSet\Services\SQLWriter
echo ------------------------------- >> %sc%
echo "for >= 11.0 version -> sqlcmd method:" >> %sc%
echo ----REG BACKUP: >> %sc%
echo REG EXPORT %regp% C:\temp\sql.reg /y >> %sc%
echo ----START >> %sc%
IF %many% equ 1 (
for /F "usebackq tokens=*" %%A in (%sfound%) do (
echo reg add %regp% /v ImagePath /d """"%scmd%""" -S %%A -E -Q """%query%"" /f >> %sc%
echo net stop SQLWriter >> %sc%
echo net start SQLWriter >> %sc% )
) else (
echo reg add %regp% /v ImagePath /d """"%scmd%""" -S %sqlc% -E -Q """%query%"" /f >> %sc%
echo net stop SQLWriter >> %sc%
echo net start SQLWriter >> %sc%
)
echo reg add %regp% /v ImagePath /d "C:\Program Files\Microsoft SQL Server\90\Shared\sqlwriter.exe" /f >> %sc%
echo net start SQLWriter >> %sc%
echo ----REG RESTORE: >> %sc%
echo REG IMPORT C:\temp\sql.reg >> %sc%
echo %query% > %cd%\psexec.sql
echo ----TEST: >> %sc%
echo to test use: >> %sc%
echo sqlcmd.exe -S %sqlc% -h -1 -E -i %cd%\chck.sql >> %sc%
echo @echo off > %tst%
echo cmd /k sqlcmd.exe -S %sqlc% -h -1 -E -i %cd%\chck.sql >> %tst%
echo.
echo done, script %sc% generated. Any key go to MENU.
echo. you can test after applaying script via test-if-iam-sysadmin.bat
PAUSE
cls
goto Menu