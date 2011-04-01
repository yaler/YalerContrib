#!/bin/sh
cp ../Library/Yaler.Net.Security.dll .  >/dev/null 2>&1
cp ../Library/Yaler.Net.Sockets.dll . >/dev/null 2>&1
gmcs /nologo TimeService.cs /r:Yaler.Net.Sockets.dll
gmcs /nologo SecureTimeService.cs /r:Yaler.Net.Security.dll
