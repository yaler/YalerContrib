@echo off
if exist time_service.exe (
	del /q time_service.exe
)
if exist tmp (
	rd /s /q tmp
)
if exist *.ncb (
	del /q *.ncb
)
if exist *.suo (
	del /q /a:H *.suo
)
if exist *.user (
	del /q *.user
)
