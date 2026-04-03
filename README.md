# Device Management System

Phase 1 - BackEnd.

Current work done:
- idempotent SQL scripts for database creation and seed data
- ASP.NET Core Web API built with .NET 10
- CRUD endpoints for users and devices
- service-layer validation and duplicate-device checks
- integration smoke tests for the API

## Tech Stack

- .NET 10
- ASP.NET Core Web API
- SQL Server / LocalDB
- Microsoft.Data.SqlClient
- xUnit

## Project Structure

- `database/create_database.sql`
- `database/seed_data.sql`
- `src/DeviceManagement.Api`
- `tests/DeviceManagement.Api.IntegrationTests`

## Prerequisites

- .NET 10 SDK
- SQL Server LocalDB or SQL Server Express
- `sqlcmd` if you want to run the SQL scripts from the terminal

## Database Setup

The API is configured for `MSSQLLocalDB`.

Default connection string:

```json
"ConnectionStrings": {
  "DeviceManagement": "Server=(localdb)\\MSSQLLocalDB;Database=DeviceManagementSystem;Integrated Security=true;Encrypt=false;TrustServerCertificate=true;"
}
```

Run the SQL scripts in this order:

```powershell
sqlcmd -S "(localdb)\MSSQLLocalDB" -d master -i .\database\create_database.sql
sqlcmd -S "(localdb)\MSSQLLocalDB" -d DeviceManagementSystem -i .\database\seed_data.sql
```

If you prefer SSMS, execute the same two files manually in the same order.

## Run The API Locally

From the repository root:

```powershell
dotnet restore .\src\DeviceManagement.Api\DeviceManagement.Api.csproj
dotnet run --project .\src\DeviceManagement.Api\DeviceManagement.Api.csproj
```

The API runs on:

- `http://localhost:5145`

## Quick Checks

You can test the current API with:

- `GET http://localhost:5145/`
- `GET http://localhost:5145/api/users`
- `GET http://localhost:5145/api/devices`

There is also a ready-to-use HTTP file here:

- `src/DeviceManagement.Api/DeviceManagement.Api.http`

## API Endpoints

### Users

- `GET /api/users`
- `GET /api/users/{id}`
- `POST /api/users`
- `PUT /api/users/{id}`
- `DELETE /api/users/{id}`

### Devices

- `GET /api/devices`
- `GET /api/devices/{id}`
- `POST /api/devices`
- `PUT /api/devices/{id}`
- `DELETE /api/devices/{id}`

## Run Tests

```powershell
dotnet test .\tests\DeviceManagement.Api.IntegrationTests\DeviceManagement.Api.IntegrationTests.csproj
```

## Current Notes

- this README documents Phase 1
