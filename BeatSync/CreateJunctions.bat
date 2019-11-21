@echo off
set SourcePath=%~1
set DestPath=%~dp0

if "%SourcePath%" == "" (
	echo Enter the full path (without quotes^) to your Beat Saber game folder. Alternatively, you can drag and drop your Beat Saber folder onto this batch file to automatically make the junctions. 
	set /p SourcePath="Path:"
)

echo Source target: %SourcePath%
echo Link target: %DestPath%
set PluginPath=%SourcePath%\Plugins
set ManagedPath=%SourcePath%\Beat Saber_Data\Managed
set LibsPath=%SourcePath%\Libs
set IPAPath=%SourcePath%\IPA

if exist "%PluginPath%" (
	echo Plugin folder exists, creating link
	if not exist "%DestPath%References" mkdir "%DestPath%References"
	mklink /J "%DestPath%References\Plugins" "%PluginPath%"
) else (
	echo Plugin folder missing
)
if exist "%ManagedPath%" (
	echo Managed folder exists, creating link
	if not exist "%DestPath%References" mkdir "%DestPath%References"
	if not exist "%DestPath%References\Beat Saber_Data" mkdir "%DestPath%References\Beat Saber_Data"
	mklink /J "%DestPath%References\Beat Saber_Data\Managed" "%ManagedPath%"
) else (
	echo Managed folder missing
)
if exist "%LibsPath%" (
	echo Libs folder exists, creating link
	if not exist "%DestPath%References" mkdir "%DestPath%References"
	mklink /J "%DestPath%References\Libs" "%LibsPath%"
) else (
	echo Libs folder missing
)
if exist "%IPAPath%" (
	echo Libs folder exists, creating link
	if not exist "%DestPath%References" mkdir "%DestPath%References"
	mklink /J "%DestPath%References\IPA" "%IPAPath%"
) else (
	echo Libs folder missing
)
:End
pause