@echo off
if exist time_service.exe (
	del /q time_service.exe
)
if exist tmp (
	rd /s /q tmp
)
if exist .vs (
	rd /s /q .vs
)
if exist *.iobj (
	del /q *.iobj
)
if exist *.ipdb (
	del /q *.ipdb
)
if exist *.ncb (
	del /q *.ncb
)
if exist *.sdf (
	del /q /a *.sdf
)
if exist *.suo (
	del /q /a *.suo
)
if exist *.user (
	del /q *.user
)
if exist *.vcxproj (
	del /q *.vcxproj
)