# Device Management System

Current progress:
- idempotent SQL scripts for database creation and seed data
- ASP.NET Core Web API built with .NET 10
- JWT authentication with register and login endpoints
- Ollama-based AI device description generation with `phi4-mini`
- ranked free-text device search across name, manufacturer, RAM, and processor
- protected CRUD endpoints for users and devices
- device self-assignment and unassignment endpoints for the authenticated user
- service-layer validation and duplicate-device checks
- a small mix of API smoke tests and device service tests
- Angular UI for Phase 4 with:
  - login page
  - register page
  - route guard and bearer-token interceptor
  - devices list and details panel
  - ranked quick search wired to the API
  - create, update, and delete flows
  - assign and unassign actions for the signed-in user
  - generate-description action in the device form

## Tech Stack

- .NET 10
- ASP.NET Core Web API
- SQL Server / LocalDB
- Microsoft.Data.SqlClient
- Ollama
- `phi4-mini`
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
- Ollama if you want to run the real AI integration locally
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

2. Optional: start Ollama with the model used by Phase 4:

Install Ollama first on Windows:

- download it from [Ollama for Windows](https://ollama.com/download/windows)
- run the installer
- or install it from PowerShell:

```powershell
irm https://ollama.com/install.ps1 | iex
```

- open a new terminal and verify with `ollama --version`

```powershell
ollama pull phi4-mini
ollama run phi4-mini
```

If Ollama is not running, the API falls back to a deterministic local description template so the feature still works.

3. Start the Angular UI in a second or third terminal:

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

Phase 4 UI flow:

- open `http://localhost:4200`
- you will land on `/login`
- sign in with the seeded account above or register a new account
- after login or register, the app redirects to `/`
- in the create or edit form, use `Generate with AI` to fill the description field

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

Optional before starting Visual Studio:

- install Ollama from [Ollama for Windows](https://ollama.com/download/windows)
- run `ollama pull phi4-mini`
- run `ollama run phi4-mini`

After startup:

- the UI opens on the auth flow first
- sign in with the seeded demo account or register a new account
- once authenticated, Visual Studio can use the same running API and Angular UI for the protected inventory dashboard
- the description generator uses Ollama when available and falls back locally when it is not

## Quick Checks

API:

- `GET http://localhost:5145/`
- `POST http://localhost:5145/api/auth/login`
- `POST http://localhost:5145/api/auth/register`
- `GET http://localhost:5145/api/devices/search?q=atlas`
- `POST http://localhost:5145/api/devices/generate-description`

UI:

- open `http://localhost:4200`
- sign in or register first
- the inventory page should load after authentication
- you should be able to create, update, and delete devices
- you should be able to assign an available device to yourself
- you should be able to unassign a device assigned to your account
- you should be able to search the inventory by name, manufacturer, RAM, and processor
- you should be able to generate a device description from the create or edit form

There is also a ready-to-use HTTP file here:

- `src/DeviceManagement.Api/DeviceManagement.Api.http`

For the protected API requests in that file:

1. run the login request first
2. copy the returned JWT token
3. paste it into `@AccessToken`
4. run the protected `users` and `devices` requests

Phase 4 note:

1. run the login request first
2. paste the JWT into `@AccessToken`
3. run the `POST /api/devices/generate-description` request
4. if Ollama is running, the response should come from `phi4-mini`
5. if Ollama is not running, the response should still succeed with `usedFallback: true`

Bonus search note:

1. sign in through the UI or paste a JWT into `@AccessToken`
2. call `GET /api/devices/search?q=...` or use the inventory quick filter
3. the search normalizes case, spacing, and punctuation
4. matching considers `Name`, `Manufacturer`, `RAM`, and `Processor`
5. stronger matches rank first, with `Name` weighted above the other searchable fields

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
- `GET /api/devices/search?q={query}`
- `GET /api/devices/{id}`
- `POST /api/devices`
- `POST /api/devices/generate-description`
- `PUT /api/devices/{id}`
- `POST /api/devices/{id}/assign`
- `POST /api/devices/{id}/unassign`
- `DELETE /api/devices/{id}`

Protected endpoints:

- all `users` endpoints require a bearer token
- all `devices` endpoints require a bearer token
- only the two `auth` endpoints are public

## Bonus Search

The bonus feature is implemented in both the API and the Angular UI.

What it does:

- accepts a free-text query
- normalizes case, repeated spaces, and punctuation
- tokenizes the query before matching
- searches these device fields:
  - `Name`
  - `Manufacturer`
  - `RAM`
  - `Processor`
- returns ranked results with stronger `Name` matches first

How to try it:

- in the UI, type into the inventory quick filter after signing in
- in the API, call `GET /api/devices/search?q=atlas`
- try variants like `ATLAS`, `atlas!!!`, or `12 gb` to see normalization and token matching

## Run Tests

```powershell
dotnet test .\DeviceManagementSystem.sln
```

## Current Notes

- this README now documents the Phase 4 setup and the bonus search feature
