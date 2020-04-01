@ECHO OFF
:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
::: 				   WK				                :::
::: 	Last update: 2020-04-01     v1.1                :::
:::                                                     :::
::: please contact: kucyk87@gmail.com         			:::
:::                        WK                           :::
:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
call variables.bat > nul
::
cd %cd%
IF "%cd%"=="C:\WINDOWS\system32" echo wrong path, start from START.bat && PAUSE && goto quit
echo.account %who% will be used (specify user inside user.txt)
echo.
echo --------------------------------------------
echo domain: %domain%
echo current hostname: %COMPUTERNAME%
for /f "tokens=1-2*" %%A in ('net statistics workstation ^| find "Statistics since"') do echo uptime: %%C
echo --------------------------------------------
:Starting
echo.1) generate scripts
echo.2) continue discovery SQLservices
echo.3) scan network for SQLservices
echo.4) exit
set menu=
choice /c 1234 /n /m "Choose a task"
set menu=%errorlevel%
if errorlevel 1 set goto=fullscript
if errorlevel 2 set goto=Scan
if errorlevel 3 set goto=Network
if errorlevel 4 set goto=quit
cls
goto %goto%

:Network
echo scan using OSQL
call OSQL -L
echo scan using powershell SqlDataSourceEnumerator:
call powershell -command "[System.Data.Sql.SqlDataSourceEnumerator]::Instance.GetDataSources()" 2> nul
echo any key to menu
pause
goto Starting

:Scan
FOR /F "tokens=* USEBACKQ" %%F IN (`tasklist ^| findstr sqlservr.exe ^| find /c /v ""`) DO ( SET sqlcount=%%F )
FOR /F "tokens=* USEBACKQ" %%F IN (`sc query ^|findstr ClusSv. ^| find /c /v ""`) DO ( SET sqlclust=%%F )
tasklist | findstr sqlservr.exe >nul && echo.SQL proccess sqlservr.exe is running. (%sqlcount% proces(s)) && set _sqlserv=1 || echo sqlservr.exe process is not running && set _sqlserv=0
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
( for /f "tokens=2*" %%a in ('REG QUERY HKLM\SYSTEM\CurrentControlSet\Services\MSSQLServer /v ImagePath') do  echo.Bin: %%b ) 2> nul && ( for /f "tokens=2*" %%a in ('REG QUERY HKLM\SYSTEM\CurrentControlSet\Services\MSSQLServer /v ImagePath') do  echo.Bin: %%b ) || ( for /f %%a in ('REG QUERY HKLM\SYSTEM\CurrentControlSet\Services\ /f *MSSQL*') do ( if not "%%a" == "End" echo %%a ) )
echo.
echo ------------------------------
echo [3/4] WMI scan...
call cscript /Nologo discovery.vbs 2> nul || echo SQL server is not detected. Skipping
( for /f "tokens=2*" %%a in ('cscript /Nologo discovery.vbs ^| find "MAJORVERSION"') DO ( set _major=%%a ) ) 2> nul
echo ------------------------------
echo.[4/4]SQLserver scan ports:
for /f "tokens=2*" %%a in ('tasklist /svc ^| findstr sqlser') do ( netstat -ano | findstr %%a | findstr LIST. )
) ELSE ( echo.sqlservr.exe process is not running )

echo ==========================================
:Menu
echo.1) generate scripts
echo.2) use browser to scan local instances
echo.3) use full WMI scan local instances (with sql 'MAJOR VERSION' %_major% from above)
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
if %_major% gtr 1 echo SQL major version is: %_major%
set /p versionwmi=SQL version (major):
IF [%versionwmi%] == [] echo.empty version, skipping && goto Menu
call cscript /Nologo discover_full.vbs %versionwmi%
goto Menu

:fullscript
setlocal enabledelayedexpansion
    set "i=0"
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
::NAMED SQL SERVICES:
  setlocal enabledelayedexpansion
    set "i=0"
	echo.
    for /f "tokens=*" %%f in ('sc query ^| findstr .MSSQL$. ^| findstr SERVICE ^| findstr /V Launcher') do (
      set arr[!i!]=%%f & set /a "i+=1"
    )
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


:ChooseSQL
echo.
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
echo ::"for <= 10.5 version -> use PSEXEC method:" > %sc%
if "%many%" equ 1 (
for /F "usebackq tokens=*" %%A in (%sfound%) do (
echo ::"%cd%\psexec.exe" -accepteula -i -s -d sqlcmd.exe -S %%A -E -i "%cd%\psexec.sql" >> %sc%)
) else (
echo ::"%cd%\psexec.exe" -accepteula -i -s -d sqlcmd.exe -S %sqlc% -E -i "%cd%\psexec.sql" >> %sc%
)
echo ::------------------------------- >> %sc%
echo ::"for >= 11.0 version -> sqlcmd method:" >> %sc%
echo ::REG BACKUP: >> %sc%
echo REG EXPORT %regp% C:\temp\sql.reg /y >> %sc%
echo ::START >> %sc%
if %many% equ 1 (
for /F "usebackq tokens=*" %%A in (%sfound%) do (
echo reg add "%regp%" /v ImagePath /d """"%scmd%""" -S %%A -E -Q """%query%"" /f >> "%sc%"
echo net stop SQLWriter >> %sc%
echo net start SQLWriter >> %sc% )
) else (
echo reg add "%regp%" /v ImagePath /d """"%scmd%""" -S "%sqlc%" -E -Q """%query%"" /f >> "%sc%"
echo net stop SQLWriter >> %sc%
echo net start SQLWriter >> %sc%
)
echo reg add %regp% /v ImagePath /d "%writer%" /f >> %sc%
echo net start SQLWriter >> %sc%
echo ::REG RESTORE: >> %sc%
echo ::REG IMPORT C:\temp\sql.reg >> %sc%
echo %query% > %cd%\psexec.sql
echo ::TEST: >> %sc%
echo ::to test use: >> %sc%
echo sqlcmd.exe -S %sqlc% -h -1 -E -i %cd%\chck.sql >> %sc%
echo @echo off > %tst%
echo cmd /k sqlcmd.exe -S %sqlc% -h -1 -E -i %cd%\chck.sql >> %tst%
echo.
echo done, fullscript .txt and .bat generated. Any key go to MENU.
echo. you can test after applaying script via test-if-iam-sysadmin.bat
copy %sc% %scb% > nul
PAUSE
call Nircmd.exe elevate cmd.exe /s /k "mode con cols=100 lines=300 && PUSHD \"
call notepad.exe fullscript.txt
cls
goto Menu
