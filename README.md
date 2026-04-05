# Device Management System

Current progress:
- idempotent SQL scripts for database creation and seed data
- ASP.NET Core Web API built with .NET 10
- JWT authentication with register and login endpoints
- protected CRUD endpoints for users and devices
- device self-assignment and unassignment endpoints for the authenticated user
- service-layer validation and duplicate-device checks
- integration smoke tests for the API
- Angular UI for Phase 3 with:
  - login page
  - register page
  - route guard and bearer-token interceptor
  - devices list and details panel
  - create, update, and delete flows
  - assign and unassign actions for the signed-in user

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

These scripts are idempotent, so they can rerun safely. That matters for Phase 3 because the database now includes the `Accounts` table and a seeded demo login.

If you prefer SSMS, execute the same two files manually in the same order.

Seeded demo account:

- email: `alexandra.ionescu@darwin.local`
- password: `Darwin123!`

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

Phase 3 UI flow:

- open `http://localhost:4200`
- you will land on `/login`
- sign in with the seeded account above or register a new account
- after login or register, the app redirects to `/`

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

After startup:

- the UI opens on the auth flow first
- sign in with the seeded demo account or register a new account
- once authenticated, Visual Studio can use the same running API and Angular UI for the protected inventory dashboard

## Quick Checks

API:

- `GET http://localhost:5145/`
- `POST http://localhost:5145/api/auth/login`
- `POST http://localhost:5145/api/auth/register`

UI:

- open `http://localhost:4200`
- sign in or register first
- the inventory page should load after authentication
- you should be able to create, update, and delete devices
- you should be able to assign an available device to yourself
- you should be able to unassign a device assigned to your account

There is also a ready-to-use HTTP file here:

- `src/DeviceManagement.Api/DeviceManagement.Api.http`

For the protected API requests in that file:

1. run the login request first
2. copy the returned JWT token
3. paste it into `@AccessToken`
4. run the protected `users` and `devices` requests

## API Endpoints

### Auth

- `POST /api/auth/register`
- `POST /api/auth/login`

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
- `POST /api/devices/{id}/assign`
- `POST /api/devices/{id}/unassign`
- `DELETE /api/devices/{id}`

Protected endpoints:

- all `users` endpoints require a bearer token
- all `devices` endpoints require a bearer token
- only the two `auth` endpoints are public

## Run Tests

```powershell
dotnet test .\tests\DeviceManagement.Api.IntegrationTests\DeviceManagement.Api.IntegrationTests.csproj
```

## Current Notes

- this README now documents the Phase 3
