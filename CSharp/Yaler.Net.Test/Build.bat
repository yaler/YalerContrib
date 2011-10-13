@echo off
set PATH=%PATH%;C:\WINDOWS\Microsoft.NET\Framework\v2.0.50727
copy /y ..\Yaler.Net\Yaler.Net.dll . > NUL
csc /nologo TimeService.cs /r:Yaler.Net.dll
csc /nologo SecureTimeService.cs /r:Yaler.Net.dll
