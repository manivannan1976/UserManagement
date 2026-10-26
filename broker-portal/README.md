# BrokerPortal

This project was generated using [Angular CLI](https://github.com/angular/angular-cli) version 20.3.2.

## Development server

To start a local development server, run:

```bash
ng serve
```

Once the server is running, open your browser and navigate to `http://localhost:4200/`. The application will automatically reload whenever you modify any of the source files.

## Code scaffolding

Angular CLI includes powerful code scaffolding tools. To generate a new component, run:

```bash
ng generate component component-name
```

For a complete list of available schematics (such as `components`, `directives`, or `pipes`), run:

```bash
ng generate --help
```

## Building

To build the project run:

```bash
ng build
```

This will compile your project and store the build artifacts in the `dist/` directory. By default, the production build optimizes your application for performance and speed.

## Running unit tests

To execute unit tests with the [Karma](https://karma-runner.github.io) test runner, use the following command:

```bash
ng test
```

## Running end-to-end tests

For end-to-end (e2e) testing, run:

```bash
ng e2e
```

Angular CLI does not come with an end-to-end testing framework by default. You can choose one that suits your needs.

## Local account authentication

The frontend no longer uses the local-storage/session-storage demo login or Auth0. It authenticates against the ASP.NET Core API using an HttpOnly, Secure cookie. The Angular app keeps only the in-memory profile; it verifies the cookie with `/api/auth/session` after navigation or refresh. In Development, Angular proxies `/api` to the local HTTPS API so the browser uses same-origin requests and avoids cross-site cookie restrictions; restart `ng serve` after changing proxy settings. In Development, login does not require an antiforgery token; production login and other state-changing requests remain protected.

Apply the additive database migration before starting the updated API:

```powershell
sqlcmd -S "YOUR_SQL_SERVER" -d BrokerPortalDb -b -i "BrokerPortal-Api-Project\BrokerPortal\BrokerPortal.Api\Database\Migrations\20261022_InvitationActivationAndAuthentication.sql"
```

This preserves existing users and adds nullable password/activation fields plus `dbo.UserInvitations`; it does not drop or recreate the existing table. This migration was applied to the configured local `BrokerPortalDb` in this workspace.

For a Development-only super-administrator bootstrap, configure ASP.NET Core User Secrets in the API project. Set a trusted administrator email and a strong development password; do not put either in source control. The bootstrap is disabled outside Development, stores only an ASP.NET Core Identity password hash, and grants the portal role only from this trusted server configuration. It creates the account only when it does not already exist; restarting the API or changing User Secrets does not reset an existing account's password or reactivate it. To explicitly reset an existing Development admin, set `DevelopmentAdmin:ResetPassword` to `true`, start the API once, then set it back to `false` before leaving the API running or restarting it.

```powershell
Set-Location "BrokerPortal-Api-Project\BrokerPortal\BrokerPortal.Api"
dotnet user-secrets set "DevelopmentAdmin:Email" "your-admin@example.test"
dotnet user-secrets set "DevelopmentAdmin:Password" "use-a-strong-local-password"
```

Use the email and password you configured above to sign in after restarting the API. There are no built-in default credentials; the earlier `admin@brokerportal.local` / `Admin@123` demo login is no longer active. Changing the bootstrap secret does not change an existing administrator password; never hard-code credentials in source control. Existing invited users must activate their accounts and choose their own passwords. Start the API with `dotnet run`, and the frontend with `npm run start` in `broker-portal`. For production, configure `FrontendOrigin` and `InvitationEmail:FrontendBaseUrl` for the deployed HTTPS frontend and use HTTPS. Configure SMTP values using User Secrets or environment variables (`InvitationEmail__SmtpHost`, `InvitationEmail__SmtpPort`, `InvitationEmail__SenderAddress`, `InvitationEmail__Username`, `InvitationEmail__Password`); never commit provider credentials.

## Portal header logo

The shared header uses the local logo asset at `public/assets/bhsi-small-commercial-logo.png`. No external logo URL is used.

## Users API and SQL Server

The user list requests paged records from the existing ASP.NET Core API at `/api/users`. During Angular development, the API base URL is `https://localhost:51273/api`; production builds use the same-origin `/api` path. Start the API from `BrokerPortal-Api-Project/BrokerPortal/BrokerPortal.Api` with `dotnet run`. The API uses EF Core to map `dbo.PortalUsers` (`UserId` maps to the API `id`, with `CreatedAt`, `UpdatedAt`, and `RowVersion` mapped to their corresponding SQL columns). The current table contract does not include an agency column, so the existing table displays `JobTitle` in that column.

The configured API appsettings uses Windows integrated authentication with the local SQL Server instance and does not contain a database password. For another SQL Server or deployment, provide the connection string through the `ConnectionStrings__BrokerPortalDb` environment variable or an ASP.NET Core user secret; do not put credentials in source control. Example PowerShell environment-variable shape (use your own server and authentication settings):

```powershell
$env:ConnectionStrings__BrokerPortalDb = "Server=YOUR_SQL_SERVER;Database=BrokerPortalDb;Trusted_Connection=True;TrustServerCertificate=True"
```

The API allows the Angular development origin `http://localhost:4200`. The API/SQL Server connection must be available for login and the live users list. In Development, the bootstrap administrator is created or refreshed from the `DevelopmentAdmin` User Secrets; otherwise, sign in with an activated account.

### Invitation and activation workflow

The Users page invites only `BROKER_ADMIN` or `BROKER_USER`. The backend creates the account with status `Invited`, generates a cryptographically random single-use token, and stores only its SHA-256 hash in `dbo.UserInvitations`. Invitation lifetime defaults to 48 hours and can be changed with `InvitationEmail__LifetimeHours`. Account activation uses ASP.NET Core Identity password hashing and atomically changes the account to `Active` while consuming the token.

When SMTP is not configured in Development, email is **not sent**. The API returns a development activation link and generated email HTML preview to an authenticated portal administrator; the Users page displays both. Outside Development, missing or failed SMTP delivery is reported as failed and no activation token is returned to the caller. Resending an invitation revokes prior unaccepted tokens. This workflow does not create an Auth0 account, provision a Duck Creek user, or imply that email was delivered when it was not.

The API endpoints are `POST /api/users/invite`, `POST /api/users/{id}/resend-invitation`, `GET /api/invitations/validate`, `POST /api/invitations/activate`, and `POST /api/auth/login`/`logout`. User listing and invitation administration require the authenticated `PORTAL_SUPER_ADMIN` role. Local bootstrap passwords, SMTP credentials, and activation tokens are never committed or stored in plaintext in SQL Server.

## Additional Resources

For more information on using the Angular CLI, including detailed command references, visit the [Angular CLI Overview and Command Reference](https://angular.dev/tools/cli) page.
