@echo off
set CSC=C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe
"%CSC%" -nologo -target:winexe -out:winmove.exe -r:System.Windows.Forms.dll -r:System.Drawing.dll winmove.cs
if %errorlevel%==0 (echo Built winmove.exe) else (echo Build failed)
