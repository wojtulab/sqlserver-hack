Dim version
if WScript.Arguments.Count = 0 then
    WScript.Echo "Missing SQLversion example: 14, 13, 12 etc."
	WScript.Echo "SQL Server 2019: 	major version is 15."
	WScript.Echo "SQL Server 2017: 	major version is 14."
	WScript.Echo "SQL Server 2016: 	major version is 13."
	WScript.Echo "SQL Server 2014: 	major version is 12."
	WScript.Echo "SQL Server 2012: 	major version is 11."
	WScript.Echo "SQL Server 2008: 	major version is 10."
	
end if
version = Left(WScript.Arguments(0), 2)
WScript.Echo "Checking version: " & version


set wmi = GetObject("WINMGMTS:\\.\root\Microsoft\SqlServer\ComputerManagement" & version)
for each prop in wmi.ExecQuery("select * from SqlServiceAdvancedProperty")
    WScript.Echo prop.ServiceName & " " & prop.PropertyName & ": " & prop.PropertyStrValue
next