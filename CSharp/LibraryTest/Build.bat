@echo off
set PATH=%PATH%;C:\WINDOWS\Microsoft.NET\Framework\v2.0.50727
copy /y ..\Library\Yaler.Net.Sockets.dll . > NUL
copy /y ..\Library\Yaler.Net.Security.dll . > NUL
csc /nologo TimeService.cs /r:Yaler.Net.Sockets.dll
csc /nologo SecureTimeService.cs /r:Yaler.Net.Security.dll

