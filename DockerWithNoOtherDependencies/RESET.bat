@echo off
setlocal
cd /d "%~dp0"

echo WARNING: This will delete the MySQL volume (all data).
pause

docker compose down -v
echo Done.
pause

endlocal