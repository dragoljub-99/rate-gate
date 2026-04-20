# RateGate

RateGate is a .NET 8 rate limiting project that evaluates incoming requests based on API key, endpoint, and policy. It supports both Token Bucket and Sliding Window Log algorithms through a single decision endpoint.

## How to download and test

1. Download **RateGate-Docker-Offline-Bundle-v1.0.1.zip** from the GitHub **Releases** section.
2. Extract it anywhere on Windows 10/11.
3. Start Docker Desktop.
4. Run `RUN_FIRST_TIME.bat`.
5. Send requests to the API at `http://localhost:5000`.

Recommended test flow:
- Check service status with `GET /health`
- Check database connectivity with `GET /health/db`
- Test the main decision endpoint with `POST /check`

Example request for Sliding Window Log:

```json
{
  "apiKey": "demo-key-1",
  "endpoint": "/sliding-demo",
  "cost": 1
}
```

Example request for Token Bucket:

```json
{
  "apiKey": "demo-key-1",
  "endpoint": "/demo",
  "cost": 1
}
```

Postman is the recommended tool for testing these requests. Swagger is also available at `http://localhost:5000/swagger` for quick inspection of the API.

The project can also be run manually in Visual Studio for development purposes.

## About the project

The solution is split into `RateGate.Api`, `RateGate.Domain`, `RateGate.Infrastructure`, and `RateGate.ConsoleDemo`. The API exposes the main `/check` endpoint plus health, debug, and admin endpoints.

`RateGate.Domain` contains the core rate limiting models and the Token Bucket implementation. `RateGate.Infrastructure` contains EF Core, MySQL persistence, DbContext, migrations, and the Sliding Window Log implementation. `RateGate.Api` wires everything together, resolves the best matching policy for the requested endpoint, and returns the final allow/deny decision.

The system evaluates each request by finding the best matching policy for the target endpoint and applying the configured rate limiting algorithm.