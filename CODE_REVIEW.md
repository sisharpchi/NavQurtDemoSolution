# Code Review Findings

## Critical functional gaps
- The mobile client expects a `POST /api/v1/auth/sign-up` endpoint, but `AuthController` is empty, so sign-up always fails with a 404/405 despite the client calling it.【F:app/NavQurt.Server.App/Services/AuthApi.cs†L22-L30】【F:src/NavQurt.Server.Web/Controllers/AuthController.cs†L5-L10】
- The client also calls `GET /api/v1/me` to fetch the current profile, yet `UsersController` exposes no actions, leaving the feature unimplemented.【F:app/NavQurt.Server.App/Services/AuthApi.cs†L76-L83】【F:src/NavQurt.Server.Web/Controllers/UsersController.cs†L5-L10】

## Data model and DTO issues
- Application DTOs use GUID identifiers (`SignUpResponse`, `AssignRoleRequest`), but `AppUser` inherits from `IdentityUser` with a string key. This mismatch will break serialization/mapping once those DTOs are used.【F:src/NavQurt.Server.Application/Dto/Auth.cs†L3-L5】【F:src/NavQurt.Server.Core/Entities/AppUser.cs†L5-L12】
- `SignUpRequest` defines properties `Firstname`/`Lastname` (lowercase "n"), which do not match the camel-cased `FirstName`/`LastName` payload emitted by the MAUI client. Model binding would therefore drop those values when the endpoint is implemented.【F:src/NavQurt.Server.Application/Dto/Auth.cs†L3】【F:app/NavQurt.Server.App/Services/AuthApi.cs†L22-L29】
- The generic repository contracts force `IEntity<int>` for most operations, preventing reuse with core identity entities (`AppUser` uses `string`, OpenIddict entities use `long`). Any attempt to reuse the abstractions for those types will fail at compile time.【F:src/NavQurt.Server.Core/Persistence/IRepository.cs†L19-L102】【F:src/NavQurt.Server.Core/Entities/AppUser.cs†L5-L12】【F:src/NavQurt.Server.Core/Entities/OpenIdApplication.cs†L5-L7】

## Infrastructure and seeding
- `IdentitySeeder` looks up an existing admin account by user name "admin" but creates it with the user name "SuperAdmin". This causes the lookup to fail on every run and the second seeding attempt to throw because the unique email already exists.【F:src/NavQurt.Server.Infrastructure/Seed/IdentitySeeder.cs†L22-L39】
- The same seeder hardcodes a production-like password (`SuperAdmin@123`). Credentials committed to source control present a security risk and should be moved to configuration or secret management.【F:src/NavQurt.Server.Infrastructure/Seed/IdentitySeeder.cs†L37】
- `MainDbContext` applies configuration from `typeof(AppDbContext).Assembly`, which is misleading at best and fragile if `AppDbContext` is removed; the intent is presumably to use the current context type.【F:src/NavQurt.Server.Infrastructure/Data/MainDbContext.cs†L24-L31】【F:src/NavQurt.Server.Infrastructure/Data/AppDbContext.cs†L5-L13】
- Only `MainDbContext` is registered in DI, yet `CompanyRepository` depends on `AppDbContext`. Without adding `AppDbContext` to the service collection, that repository cannot be resolved.【F:src/NavQurt.Server.Web/Extensions/DbContextExtensions.cs†L8-L16】【F:src/NavQurt.Server.Infrastructure/Persistence/CompanyRepository.cs†L10-L71】

## Maintainability concerns
- Nearly every C# file contains a UTF-8 BOM character (`﻿`) at the beginning (visible as `﻿` in diffs). This pollutes diffs and can break tooling; consider normalizing the encoding via `.editorconfig` or repository settings.【F:src/NavQurt.Server.Web/Controllers/AuthController.cs†L1-L10】【F:src/NavQurt.Server.Infrastructure/Persistence/GenericRepository.cs†L1-L75】
- README.md only contains the project title without setup or usage instructions, which makes onboarding difficult.【F:README.md†L1】

## Tooling status
- `dotnet build` could not be executed in the review environment because the .NET SDK is not installed, so runtime issues may still exist.【05cf64†L1-L3】

