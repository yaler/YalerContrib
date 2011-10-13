#!/bin/sh
cp ../Yaler.Net/Yaler.Net.dll .  >/dev/null 2>&1
gmcs /nologo TimeService.cs /r:Yaler.Net.dll
gmcs /nologo SecureTimeService.cs /r:Yaler.Net.dll
