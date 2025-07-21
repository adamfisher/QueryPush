@echo off
setlocal

set SERVICE_NAME=QueryPush
set INSTALL_DIR=C:\Program Files\QueryPush

echo Installing QueryPush as Windows Service...

if not exist "%INSTALL_DIR%" mkdir "%INSTALL_DIR%"
copy /Y *.* "%INSTALL_DIR%\"

sc create %SERVICE_NAME% binPath="\"%INSTALL_DIR%\QueryPush.exe\" --service" start=auto
sc description %SERVICE_NAME% "QueryPush Database Query Scheduler"
sc start %SERVICE_NAME%

echo QueryPush service installed and started
echo Use 'sc query QueryPush' to check status
pause
