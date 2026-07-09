# RateGate

Backend API for evaluating incoming requests by API key, endpoint, and configured rate-limiting policy.

## Download and run

Requirements:

* Windows 10 or Windows 11
* Docker Desktop

Steps:

1. Download the latest `RateGate-Docker-Offline-Bundle-*.zip` from the GitHub **Releases** section.
2. Extract the archive.
3. Start Docker Desktop.
4. Run `RUN_FIRST_TIME.bat`.
5. Open Swagger at `http://localhost:5000/swagger`.

Use `RUN.bat` to start the containers again and `STOP.bat` to stop them.
`RESET.bat` stops the containers and deletes the MySQL data volume.

The docker bundle runs the API and MySQL locally. You do not need to install .NET SDK, install MySQL or configure the database manually to review the project through Docker.

## Test the API

Check the service and database:

* `GET /health`
* `GET /health/db`

Test the main decision endpoint:

```http
POST /check
Content-Type: application/json
```

Sliding Window Log example:

```json
{
  "apiKey": "demo-key-1",
  "endpoint": "/sliding-demo",
  "cost": 1
}
```

Token Bucket example:

```json
{
  "apiKey": "demo-key-1",
  "endpoint": "/demo",
  "cost": 1
}
```

Repeat the requests quickly to observe allowed and denied decisions. Requests can be tested through Swagger or Postman.
A successful request returns `"allow": true`. After enough repeated requests the API returns `"allow": false` with rate limit information.

Run the automated tests from the source repository:

```bash
dotnet test RateGate.sln
```

## What this project demonstrates

This project was built as a backend portfolio project to demonstrate REST API design, rate-limiting algorithms, persistence with EF Core/MySQL, Docker based local setup, Swagger/OpenAPI documentation and automated testing.

## About the project

RateGate exposes a single decision endpoint that validates an API key, resolves the best matching policy for the requested endpoint, and applies the configured rate-limiting algorithm.

The project implements **Token Bucket** and **Sliding Window Log** algorithms. Token Bucket state is managed in memory, while Sliding Window Log usage is stored in MySQL through Entity Framework Core.

The solution separates API, domain logic, persistence, console demonstration, and automated tests. It also includes basic admin endpoints for managing users, API keys, policies, and usage metrics.

Unit and integration tests cover request validation, policy resolution, Token Bucket behavior, and Sliding Window Log persistence.

**Technologies:** .NET 8, ASP.NET Core, Entity Framework Core, MySQL, xUnit, Docker and Swagger.
