RateGate - Offline Docker Bundle (Windows 10/11)

What you need:
- Windows 10 or Windows 11
- Docker Desktop installed and running

How to run (first time on this machine):
1) Unzip this folder anywhere.
2) Start Docker Desktop and wait until it shows "Running".
3) Double-click RUN_FIRST_TIME.bat
4) Open Swagger in your browser:
   http://localhost:5000/swagger

Useful URLs:
- Health:    http://localhost:5000/health
- DB Health: http://localhost:5000/health/db
- Swagger:   http://localhost:5000/swagger

Seeded demo data:
- API key: demo-key-1
- Policies include:
  - "*" => TokenBucket
  - "/sliding-demo" => SlidingWindowLog

Try it quickly (in Swagger or Postman):
POST /check
{
  "apiKey": "demo-key-1",
  "endpoint": "/sliding-demo",
  "cost": 1
}

Or TokenBucket demo via wildcard policy:
POST /check
{
  "apiKey": "demo-key-1",
  "endpoint": "/demo",
  "cost": 1
}

Stop containers:
- STOP.bat

Start again later:
- RUN.bat

Reset EVERYTHING (delete DB data too):
- RESET.bat