@echo off
set CSC=C:\WINDOWS\Microsoft.NET\Framework\v4.0.30319\csc.exe
copy /y ..\Yaler.Net\Yaler.Net.dll . > NUL
%CSC% /nologo TimeService.cs /r:Yaler.Net.dll
%CSC% /nologo SecureTimeService.cs /r:Yaler.Net.dll
