Dim strValueName, strSKUName, strPATH, strEdition, strVersion, strArchitecture
Dim objWMI, objProp

On Error Resume Next
' First try SQL Server 2008/2008 R2:
' WScript.Echo "Looking for 2008"
Set objWMI = GetObject("WINMGMTS:\\.\root\Microsoft\SqlServer\ComputerManagement11")
' WScript.Echo Err.Number
If Err.Number <> 0 Then
    ' Next, try SQL Server 2016:
	' WScript.Echo "Looking for 2016"
    Set objWMI = GetObject("WINMGMTS:\\.\root\Microsoft\SqlServer\ComputerManagement12")
    ' WScript.Echo Err.Number
	If Err.Number <> 0 Then
        ' Next, try SQL Server 2012:
		' WScript.Echo "Looking for 2012"
        Set objWMI = GetObject("WINMGMTS:\\.\root\Microsoft\SqlServer\ComputerManagement13")
		' WScript.Echo Err.Number
			If Err.Number <> 0 Then
			' Next, try SQL Server 2012:
			' WScript.Echo "Looking for 2012"
			Set objWMI = GetObject("WINMGMTS:\\.\root\Microsoft\SqlServer\ComputerManagement14")
			' WScript.Echo Err.Number
		End If
    End If
End If

' WScript.Echo "Znalezione bledy: "
' WScript.Echo Err.Number

If Err.Number <> 0 Then

    On Error Goto 0
    ' Go through the properties (which is just one) and find the name of the SKU.
    For Each objProp In objWMI.ExecQuery("select * from SqlServiceAdvancedProperty where SQLServiceType = 1 AND (PropertyName = 'SKUNAME' OR PropertyName = 'VERSION' OR PropertyName = 'DATAPATH')")
        If objProp.PropertyName = "SKUNAME" THEN
            strSKUName = objProp.PropertyStrValue
        Elseif objProp.PropertyName = "VERSION" THEN
            strVersion = objProp.PropertyStrValue
		Else
			strPATH = objProp.PropertyStrValue
        End If
    Next
		

    ' We do not want the number of bits, so chop it off!
    If Instr(strSKUName, " (") <> 0 Then
        strEdition = Left(strSKUName, Instr(strSKUName, " ("))
        strArchitecture = "64-bit"
    Else
        strEdition = strSKUName
        strArchitecture = "32-bit"
    End If

	WScript.Echo "---=== MAJOR VERSION ===--- : " & Left(strVersion, 2)
	WScript.Echo "Version: " & strVersion
	WScript.Echo "Path: " & strPATH
    WScript.Echo "Edition: " & strEdition & " / " & strSKUName & " / " & strArchitecture

End If