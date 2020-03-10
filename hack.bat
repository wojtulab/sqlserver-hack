@ECHO OFF
:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
::: 				   WK				                :::
::: 	Last update: 2020-03-09     v6                  :::
:::                                                     :::
::: please contact: kucyk87@gmail.com         			:::
:::                        WK                           :::
:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
cd %cd%
IF "%cd%"=="C:\WINDOWS\system32" echo wrong path, run this script from cmd.exe as admin (just open cmd and call this script) & goto exitting
set host=%COMPUTERNAME%
echo is current path correct?
echo %cd%
echo.
echo and running with admin rights? (ctrl+c to cancel)
PAUSE
cls
echo.
echo ------------------------------
echo current hostname: %COMPUTERNAME%
echo ------------------------------
:Scan
sc query |findstr .SQLB. >nul && Echo.SQLBrowser is running. && set _browser=1 || Echo.SQLBrowser is disabled. && set _browser=0
tasklist | findstr sqlservr.exe >null && echo.SQL proccess sqlservr.exe is running. && set _sqlserv=1 || echo sqlservr.exe process is not running && set _sqlserv=0
if %_sqlserv% equ 1 (
echo ------------------------------
echo.registry scan...
for /f "tokens=2*" %%a in ('REG QUERY HKLM\SYSTEM\CurrentControlSet\Services\MSSQLServer /v DisplayName') do echo service name: %%b
echo.
for /f "tokens=2*" %%a in ('REG QUERY HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\MSSQLServer\Setup /v SqlPath') do echo.Path: %%b
echo.
for /f "tokens=2*" %%a in ('REG QUERY HKLM\SYSTEM\CurrentControlSet\Services\MSSQLServer /v ImagePath') do  echo.Bin: %%b
echo.
echo.SQLserver ports:
for /f "tokens=2*" %%a in ('tasklist /svc ^| findstr sqlser') do ( netstat -ano | findstr %%a | findstr LIST. )
) ELSE ( echo.sqlservr.exe process is not running )
echo ==========================================
:Menu
echo.1) generate scripts
echo.2) exit
set menu=
choice /c 12 /n /m "Choose a task"
set menu=%errorlevel%
if errorlevel 1 set goto=fullscript
if errorlevel 2 set goto=quit
cls
goto %goto%

:quit
exit

:fullscript
::Scanning for SQL services:
::sc query | findstr /R [a-z]*MSSQLSERVER | findstr SERVICE | findstr /V Launcher >nul && ( goto Default)
::sc query | findstr .MSSQL$. | findstr SERVICE | findstr /V Launcher >nul && ( goto Named )
setlocal enabledelayedexpansion
    ::incremental varibale
    set "i=0"
    ::store filenames into array
    echo Default MsSQL instances:
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
	echo Founded SQL services: !i!
  endlocal
  ::another way to create array arr.!i!=%%f
::NAMED SQL SERVICES:
  setlocal enabledelayedexpansion
    ::incremental varibale
    set "i=0"
    ::store filenames into array
	echo.
	echo Named MsSQL instances:
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
	echo %COMPUTERNAME%%snamed%
	set /a "i+=1"
	if %i% neq %len% goto:loop2
	:wyniknam
	 echo Founded SQL services: !i!
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
echo.1) use default: %COMPUTERNAME%
echo.2) use named sql: %sqlc%
echo.3) use another instance...
echo.4) use browser to scan local instances
echo.5) scan for open ports if process running
set menu=
choice /c 1234 /n /m "Choose a task"
set menu=%errorlevel%
if errorlevel 1 set goto=Nextstuff1
if errorlevel 2 set goto=Nextstuff2
if errorlevel 3 set goto=Specified
if errorlevel 4 set goto=browser
goto %goto%

:browser
cls
echo scanning for sql services...
if %_browser% equ 1 (call PortQry.exe -n localhost -p udp -o 1434 | findstr "ServerName InstanceName tcp Version") ELSE (echo.SQLBrowser is disabled.)

goto ChooseSQL

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
call PortQry.exe -n localhost -p udp -o 1434 | findstr "Version"
echo.
echo "<= 10.5 version -> using psexec method from generated script"
echo ">= 11.0 version -> using writer method from generated script"
echo.
echo ----------------REMEMBER ----------------------------------
) 
set sc=..\fullscript.txt
echo "for <= 10.5 version -> use PSEXEC method:" >> %sc%
echo "%cd%\psexec.exe" -accepteula -i -s -d sqlcmd.exe -S %sqlc% -E -i %cd%\psexec.sql >> %sc%
FOR /F "tokens=* USEBACKQ" %%F IN (`where SQLCMD.exe`) DO ( SET scmd=%%F )
FOR /F "tokens=* USEBACKQ" %%F IN (`whoami`) DO ( SET who=%%F )
set who=%who: =%
set query=CREATE LOGIN [%who%] from windows; ALTER SERVER ROLE sysadmin ADD MEMBER [%who%];
set regp=HKLM\SYSTEM\CurrentControlSet\Services\SQLWriter
echo ------------------------------- >> %sc%
echo "for >= 11.0 version -> sqlcmd method:" >> %sc%
echo REG EXPORT %regp% C:\temp\sql.reg /y >> %sc%
echo ----START >> %sc%
echo reg add %regp% /v ImagePath /d """"%scmd%""" -S %sqlc% -E -Q """%query%"" /f >> %sc%
echo net stop SQLWriter >> %sc%
echo net start SQLWriter >> %sc%
echo reg add %regp% /v ImagePath /d "C:\Program Files\Microsoft SQL Server\90\Shared\sqlwriter.exe" /f >> %sc%
echo net start SQLWriter >> %sc%
echo ----STOP >> %sc%
echo REG IMPORT C:\temp\sql.reg >> %sc%
echo %query% > %cd%\psexec.sql
echo ----TEST: >> %sc%
echo to test use: >> %sc%
echo sqlcmd.exe -S %sqlc% -h -1 -E -i %cd%\chck.sql >> %sc%
echo @echo off > ..\test-if-iam-sysadmin.bat
echo cmd /k sqlcmd.exe -S %sqlc% -h -1 -E -i %cd%\chck.sql >> ..\test-if-iam-sysadmin.bat
echo.
echo done, script %sc% generated. Any key go to MENU.
PAUSE
cls
goto Menu