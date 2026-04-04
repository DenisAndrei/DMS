# Device Management System

Current progress:
- idempotent SQL scripts for database creation and seed data
- ASP.NET Core Web API built with .NET 10
- CRUD endpoints for users and devices
- service-layer validation and duplicate-device checks
- integration smoke tests for the API
- Angular UI for Phase 2 with:
  - devices list
  - device details panel
  - create device flow
  - update device flow
  - delete action directly from the list

## Tech Stack

- .NET 10
- ASP.NET Core Web API
- SQL Server / LocalDB
- Microsoft.Data.SqlClient
- Angular 21
- TypeScript
- xUnit

## Project Structure

- `database/create_database.sql`
- `database/seed_data.sql`
- `src/DeviceManagement.Api`
- `src/DeviceManagement.Ui`
- `tests/DeviceManagement.Api.IntegrationTests`

## Prerequisites

- .NET 10 SDK
- SQL Server LocalDB or SQL Server Express
- Node.js and npm
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

## Run Locally From The Terminal

From the repository root:

1. Start the API:

```powershell
dotnet restore .\src\DeviceManagement.Api\DeviceManagement.Api.csproj
dotnet run --project .\src\DeviceManagement.Api\DeviceManagement.Api.csproj
```

The API is configured to run locally on:

- `http://localhost:5145`

2. Start the Angular UI in a second terminal:

```powershell
cd .\src\DeviceManagement.Ui
npm install
npm start
```

If PowerShell blocks `npm`, run:

```powershell
npm.cmd install
npm.cmd start
```

The Angular UI runs locally on:

- `http://localhost:4200`

The UI proxies `/api/*` requests to:

- `http://localhost:5145`

## Run Locally In Visual Studio

1. Open `DeviceManagementSystem.sln`
2. Set multiple startup projects
3. Start:
   - `DeviceManagement.Api`
   - `DeviceManagement.Ui`
4. Leave the integration test project as `None`

Recommended debug targets:

- `DeviceManagement.Api` -> `http`
- `DeviceManagement.Ui` -> `npm start`

## Quick Checks

API:

- `GET http://localhost:5145/`
- `GET http://localhost:5145/api/users`
- `GET http://localhost:5145/api/devices`

UI:

- open `http://localhost:4200`
- the inventory page should load devices from the API
- you should be able to create, update, and delete devices from the UI

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

- this README currently documents Phase 1 and Phase 2