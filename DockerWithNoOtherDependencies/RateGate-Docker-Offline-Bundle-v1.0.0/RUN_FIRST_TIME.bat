@echo off
setlocal

echo ===========================================
echo  RateGate Offline Docker Bundle - First Run
echo ===========================================
echo.
echo This will:
echo  1) Load Docker images from .tar files
echo  2) Start MySQL + RateGate API via docker compose
echo.

REM Ensure we are in the bundle folder
cd /d "%~dp0"

echo [1/2] Loading Docker images...
docker load -i "%cd%\images\rategate-api_v1.0.0.tar"
if errorlevel 1 (
  echo ERROR: Failed to load rategate-api image.
  exit /b 1
)

docker load -i "%cd%\images\mysql_8.0.tar"
if errorlevel 1 (
  echo ERROR: Failed to load mysql image.
  exit /b 1
)

echo.
echo [2/2] Starting containers...
docker compose up -d
if errorlevel 1 (
  echo ERROR: docker compose failed.
  echo Make sure Docker Desktop is installed and running.
  exit /b 1
)

echo.
echo Done.
echo Swagger:  http://localhost:5000/swagger
echo Health:   http://localhost:5000/health
echo DB:       http://localhost:5000/health/db
echo.
echo Seeded apiKey: demo-key-1
echo Try POST /check with endpoint /sliding-demo or /demo
echo.

endlocal
