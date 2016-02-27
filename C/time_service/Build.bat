@echo off
msbuild TimeService.sln /t:Clean;Rebuild /v:quiet /nologo
