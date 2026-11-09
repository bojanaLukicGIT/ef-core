# EfCoreMiniApp

A mini app (Book/Author/Publisher) for EF Core 8 Fundamentals: .NET 8, EF Core 8, ASP.NET Core Web API, SQL Server, xUnit.

## Structure

- `src/EfCoreMiniApp.Domain` – entities (`Author`, `Book`, `Publisher`, `Artist`, `Cover`, `CoverAssignment`, `AuthorByArtist`), `Address`, `ContactDetails`, `BookGenre`
- `src/EfCoreMiniApp.Data` – `PubContext`, Fluent API configurations, seed data, migrations, interceptors
- `src/EfCoreMiniApp.Api` – Minimal API endpoints, DTO models, validation, Swagger
- `tests/EfCoreMiniApp.Tests` – xUnit tests (SQLite in-memory, API integration tests, SQL Server tests)

## Prerequisites

- .NET SDK 8 or later (the projects target `net8.0`, so the .NET 8 runtime is required)
- SQL Server LocalDB (`(localdb)\MSSQLLocalDB`) or another SQL Server

The connection string is in `src/EfCoreMiniApp.Api/appsettings.json` (`ConnectionStrings:PubConnection`, Windows authentication). It can be overridden with the `ConnectionStrings__PubConnection` environment variable.

## Running

```bash
dotnet tool restore
```

```bash
dotnet ef database update --project src/EfCoreMiniApp.Data --startup-project src/EfCoreMiniApp.Api
```

```bash
dotnet run --project src/EfCoreMiniApp.Api --launch-profile http
```

Swagger: http://localhost:5080/swagger

## Tests

```bash
dotnet test
```

Tests marked `Category=SqlServer` create and drop the `EfCoreMiniAppTests` database on LocalDB (to use a different server, set the `EFCOREMINIAPP_TEST_SQLSERVER` environment variable). Without SQL Server:

```bash
dotnet test --filter "Category!=SqlServer"
```
