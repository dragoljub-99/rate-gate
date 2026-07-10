@echo off
setlocal
cd /d "%~dp0"

echo Starting containers...
docker compose up -d
echo Done.
echo Swagger: http://localhost:5000/swagger

endlocal