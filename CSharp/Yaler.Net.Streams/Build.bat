@echo off
set CSC=C:\WINDOWS\Microsoft.NET\Framework\v4.0.30319\csc.exe
%CSC% /nologo /t:library /out:Yaler.Net.Streams.dll Yaler.Net.Streams.AssemblyInfo.cs Yaler.Net.Streams.cs /r:../Yaler.Net/Yaler.Net.dll %*
