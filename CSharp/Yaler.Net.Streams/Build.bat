@echo off
set PATH=%PATH%;C:\WINDOWS\Microsoft.NET\Framework\v2.0.50727
csc /nologo /t:library /out:Yaler.Net.Streams.dll Yaler.Net.Streams.AssemblyInfo.cs Yaler.Net.Streams.cs /r:../Yaler.Net/Yaler.Net.dll %*
