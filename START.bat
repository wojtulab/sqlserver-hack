@echo off
echo STARTING with UAC, don't close this window.
SET var=%cd%
Nircmd.exe elevate cmd.exe /s /k "PUSHD %var% && hack.bat"