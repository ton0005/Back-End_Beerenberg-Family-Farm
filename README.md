# Farm Management — Back End (Beerenberg Family Farm)

This repository contains the backend services for the Farm Management system used by Beerenberg Family Farm.
It is an ASP.NET Core Web API solution providing endpoints for authentication, staff management, shifts, time entries, payroll, and audit trails.

Key projects
- `src/FarmManagement.Api` — main Web API (entry point)
- `src/FarmManagement.Application` — application layer, DTOs and business logic
- `src/FarmManagement.Infrastructure` — data access, EF Core, migrations and services

## Table of contents
- [Status](#status)
- [Prerequisites](#prerequisites)
- [Quick start (run locally)](#quick-start-run-locally)
- [Configuration](#configuration)
- [Database & Migrations](#database--migrations)
- [API surface / Documentation](#api-surface--documentation)
- [Testing](#testing)
- [CI / CD](#ci--cd)
- [Contributing](#contributing)
- [License and contacts](#license-and-contacts)

## Status

This project targets .NET 8 (see `src/FarmManagement.Api/FarmManagement.Api.csproj`). Active development — use with caution in production until validated.

## Prerequisites

- .NET SDK 8.0 installed (the API project targets `net8.0`).
- A SQL Server or compatible database for data storage (connection configured in `appsettings.json`).
- (Optional) `dotnet-ef` tools if you plan to apply or create EF Core migrations locally:

  dotnet tool install --global dotnet-ef

## Quick start (run locally)

1. Clone the repository:

	git clone <repo-url>
	cd "Back End_Beerenberg Family Farm"

2. Configure a connection string in `src/FarmManagement.Api/appsettings.json` (or use `appsettings.Development.json` for local dev). See the Configuration section below.

3. Restore, build and run the API (from repository root):

	dotnet restore
	dotnet build --configuration Debug
	dotnet run --project src/FarmManagement.Api

4. By default the API exposes Swagger/OpenAPI when running in Development. Open a browser to:

	https://localhost:5001/swagger/index.html

Adjust port/URLs depending on your environment (check `launchSettings.json` in the API project).

## Configuration

- `src/FarmManagement.Api/appsettings.json` — main application settings. The repo also contains a root `appsettings.json` used for CI/ops.
- Typical values to set before running locally:
  - `ConnectionStrings:DefaultConnection` — database connection string
  - JWT / authentication keys and tokens (see security settings in `src/FarmManagement.Application` and `src/FarmManagement.Infrastructure`)

Note: Don't commit secrets. Use development secrets, environment variables, or your secret manager for local runs.

## Database & Migrations

Database migrations and EF Core are in the `src/FarmManagement.Infrastructure` project. Typical workflow:

1. From repository root, run migrations (ensure `dotnet-ef` is installed):

	dotnet ef database update --project src/FarmManagement.Infrastructure --startup-project src/FarmManagement.Api

2. To add a migration:

	dotnet ef migrations add <MigrationName> --project src/FarmManagement.Infrastructure --startup-project src/FarmManagement.Api

Replace startup/project paths as needed when running from different working directories.

## API surface / Documentation

Controllers live under `src/FarmManagement.Api/Controllers` and include (non-exhaustive):

- `LoginController` — authentication and token endpoints
- `StaffController` — staff CRUD and management
- `ShiftsController` / `PublicShiftsController` — shift creation and assignments
- `TimeEntriesController` — clock-in/clock-out and session edits
- `PayrollController` — payroll runs and line items
- `AuditController` — audit queries

Use the running Swagger UI (`/swagger`) to explore endpoints and request/response models. The repository also contains an `API_CONTRACT.md` at the root with contract details.

## Testing

This solution contains application and infrastructure layers prepared for unit/integration testing. To run tests (if present) use the appropriate `dotnet test` command for the test projects once added.

## CI / CD

An Azure Pipelines YAML (`azure-pipelines.yml`) is included for build/CI. Modify the pipeline as needed for your organization.

## Contributing

1. Open an issue describing the bug or feature.
2. Create a feature branch from `main`.
3. Add clear commit messages and tests for new behavior where applicable.
4. Create a pull request and request reviews.

Code style follows default C# conventions and the project uses nullable reference types (`<Nullable>enable</Nullable>`).

## License and contacts

Add your license here (e.g., MIT) or create a `LICENSE` file in the repository.

For questions contact the repository owner or the maintainers listed in the repo.

---

