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

## Delegated administration (Requirements/User-Management-Requirements.pdf)

Apply the SQL scripts in `BrokerPortal.Api/Database/Migrations` in order. `20261026_AgenciesAndDelegatedAdministration.sql` adds the `Agencies` table, `PortalUsers.AgencyId`/`DeactivatedAt`, and three sample agencies standing in for the Duck Creek agency directory.

| Requirement | Implementation |
| --- | --- |
| 2 Roles | `PORTAL_SUPER_ADMIN`, `UNDERWRITER`, `UNDERWRITER_MANAGER`, `BROKER_ADMIN`, `BROKER_USER` (`PortalRoles`). Broker admins can only invite `BROKER_USER`; internal roles are never available to them. |
| 4.A Onboarding | Super admin invites a broker admin against an agency; the email must be on one of the agency's accepted domains. `GET /api/agencies`. |
| 4.B Invitation and activation | Links expire after `InvitationEmail:LifetimeHours` (default 168 = 7 days, Q5). Expired, reused or revoked links show a clear message. Activation confirms first and last name. After sign-in the user's workspace is derived from role and agency (`Workspaces` settings). |
| 4.C Inviting users | Agency is inherited for broker admins. `POST /api/users/invite/bulk` invites up to 500 emails per request; each row succeeds or fails independently. |
| 4.D List and statuses | Broker admins only see their own agency, enforced in the API query. Statuses: Invited, Active, Expired (set automatically when every invitation has lapsed), Deactivated, Setup Incomplete. |
| 4.E Maintenance | `PUT /api/auth/profile` edits name and job title only; email and role cannot change. Deactivate requires `{"confirm": true}`; the session cookie is re-validated against the database on every request so access ends immediately. Reactivate keeps the same account and history. |
| Q2 | `Agencies.MaxActiveUsers` (null = unlimited) is enforced on invite and reactivate. |
| Q4 | `Session:IdleTimeoutMinutes` (default 480). |

Not implemented because they need external systems: Auth0 SSO sign-in (users currently set a local password on activation), creating and deactivating users in Auth0 and Duck Creek, the "Setup incomplete" transition from a failed Duck Creek setup, and two-way sync of internal users with S|2 (Q7).

## Duck Creek User Admin

Every portal user (broker admin, broker user, underwriter) is also created in Duck Creek User Admin when the invitation is created. `IDuckCreekUserAdminClient` has two implementations, selected by `DuckCreek:Mode`:

| Mode | Behaviour |
| --- | --- |
| `Local` (default) | Stores the user in the `DuckCreekUsers` table of the portal database. Behaves like the real API, including `FailIfExists`. `GET /api/duckcreek/users` (super admin) lists them; password hashes are never returned. |
| `Http` | Calls `POST {BaseUrl}/ServerPolicy/api/v1/users?UserName=&FullName=&Password=&FailIfExists=&entityId=` with Basic auth. Untested until a Duck Creek environment is available. |

To switch: set `DuckCreek:Mode` to `Http` and provide `DuckCreek:BaseUrl`, `DuckCreek:Username` and `DuckCreek:Password` through User Secrets or environment variables. Never put the Duck Creek credentials in `appsettings.json` or commit them.

- The Duck Creek entity comes from `Agencies.DuckCreekEntityId` (Amwins US is 10021; the other sample ids are placeholders). Internal users use `DuckCreek:InternalEntityId`.
- The Duck Creek user name is the portal email. The Duck Creek password is random and never stored by the portal, because users sign in through the portal.
- If provisioning fails (including an agency with no entity id) the portal user is saved as **Setup Incomplete**, no invitation is emailed, and **Resend** retries. The call uses `FailIfExists=false`, so retries are safe.
- Not yet synced to Duck Creek: name changes after activation, deactivation and reactivation. The Duck Creek collection only covers user creation.
- Apply `Database/Migrations/20261026_DuckCreekUserAdminLocal.sql`.
