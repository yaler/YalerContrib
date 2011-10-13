#!/bin/sh
gmcs /nologo /t:library /out:Yaler.Net.dll Yaler.Net.AssemblyInfo.cs Yaler.Net.Streams.StreamHelper.cs Yaler.Net.Sockets.SocketHelper.cs Yaler.Net.Sockets.HttpParser.cs Yaler.Net.Sockets.DigestContext.cs Yaler.Net.Sockets.NtlmContext.cs Yaler.Net.Sockets.ProxyClient.cs Yaler.Net.Sockets.YalerListener.cs Yaler.Net.Security.YalerSslListener.cs
