# NavQurt Demo Solution

## Overview
NavQurt is split into a .NET multi-project backend hosted in `src/` and a MAUI client hosted in `app/`. The server exposes authentication endpoints backed by ASP.NET Core Identity and OpenIddict, while the client exercises those endpoints for sign-up and token management scenarios.

## Prerequisites
- .NET 7 SDK or later
- PostgreSQL 14 or later
- Node.js 18+ (for building frontend assets, if any project requires it)

## Configuration
Connection strings and seed credentials are read from configuration (appsettings, environment variables, or user secrets). At a minimum, the following keys must be provided:

```json
{
  "ConnectionStrings": {
    "Default": "Host=localhost;Database=navqurt;Username=postgres;Password=postgres"
  },
  "Seed": {
    "AdminUserName": "SuperAdmin",
    "AdminEmail": "superadmin@qurt.local",
    "AdminPassword": "<set a secure password>",
    "AdminPhone": "+998900000000"
  }
}
```

The seeder will skip creating the administrator account if `Seed:AdminPassword` is not supplied.

Optional overrides are available for `Seed:AdminFirstName` and `Seed:AdminLastName`.

## Database Setup
1. Create the database defined in the `ConnectionStrings:Default` setting.
2. Apply the Entity Framework Core migrations:
   ```bash
   dotnet ef database update --project src/NavQurt.Server.Infrastructure --startup-project src/NavQurt.Server.Web
   ```
3. Run the web project once to execute the identity seeder.

## Running the Server
```bash
dotnet run --project src/NavQurt.Server.Web
```
The API listens on the URLs defined in the project's `launchSettings.json` or the `ASPNETCORE_URLS` environment variable.

## Running the MAUI Client
```bash
dotnet build app/NavQurt.Server.App
```
Additional platform-specific commands may be required (Android/iOS emulators, Windows packaging, etc.). Refer to the official MAUI documentation for details.

## Troubleshooting
- Ensure PostgreSQL is running and reachable via the configured connection string.
- Confirm the admin password is supplied before running the seeder; otherwise the initial admin account will not be created.
- Review application logs for seeding warnings or errors.
