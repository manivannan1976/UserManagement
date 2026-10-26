# BrokerPortal API

A new .NET 8 Web API solution for the Angular broker portal user-list feature.
This project does not modify or reference the existing CustomAgencyImport DLL.

## Prerequisites
- .NET 8 SDK
- SQL Server LocalDB on Windows, or update the connection string for your SQL Server instance

## Run
```bash
dotnet restore
dotnet build
dotnet run --project BrokerPortal.Api
```

Open `/swagger` on the URL printed by the app.

## Database
Install EF CLI if needed:
```bash
dotnet tool install --global dotnet-ef --version 8.0.0
```

Create and apply migration:
```bash
dotnet ef migrations add InitialCreate --project BrokerPortal.Infrastructure --startup-project BrokerPortal.Api --output-dir Data/Migrations
dotnet ef database update --project BrokerPortal.Infrastructure --startup-project BrokerPortal.Api
```

## Endpoint
`GET /api/users?search=john&status=Active&page=1&pageSize=10`

The endpoint returns paginated users from SQL Server. The database is initially empty.

## Important
This is a starter implementation for local development. Before production, configure authentication/authorization, agency-level data isolation, audit logging, secure connection strings, approved patched package versions, and Auth0/Duck Creek integration contracts. No Auth0 or Duck Creek calls are made yet.
