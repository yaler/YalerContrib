@echo off
set PATH=%PATH%;C:\WINDOWS\Microsoft.NET\Framework\v2.0.50727
csc /nologo /t:library Yaler.Net.Sockets.cs
csc /nologo /t:library Yaler.Net.Security.cs

