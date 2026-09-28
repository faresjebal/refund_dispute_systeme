# Refund and Dispute Management System

An ASP.NET Core internship API for recording transactions, submitting refund requests and disputes, and letting authorized staff process them. The repository contains SQL Server persistence through Entity Framework Core and audit logs. An Angular starter project is also present, but the refund/dispute frontend is not implemented in that client. It is a portfolio copy of work completed during an internship at MS Solution; it is not presented as a production deployment.

## What is here

| Project | Role |
| --- | --- |
| `Internship.Api` | HTTP endpoints, authentication, authorization, Swagger, and startup |
| `Internship.Application` | DTOs, interfaces, and business services |
| `Internship.Domain` | Entities and domain interfaces |
| `Internship.Infrastructure` | EF Core context, migrations, repositories, token and email services |
| `Internship.Web/ClientApp` | Angular starter scaffold; no verified refund/dispute UI integration |

The API exposes authentication, transactions, refund requests, disputes, and audit log controllers. Refund and dispute workflows include user submissions and staff processing. Consult the controllers and services for the exact endpoints and current behavior.

## Local setup

Prerequisites for the API: .NET 9 SDK and SQL Server or SQL Server LocalDB. The checked-in connection string targets Windows LocalDB. On another system, override it with a SQL Server connection string.

1. Clone the repository and enter its root.
2. Set configuration locally. Do **not** commit real secrets. The API requires a JWT signing key and the email service expects a SendGrid API key. For local development, use .NET user secrets from `Internship.Api`:

   ```bash
   dotnet user-secrets set "JwtSettings:Secret" "REPLACE_WITH_A_NEW_RANDOM_SECRET_AT_LEAST_32_BYTES"
   dotnet user-secrets set "SendGrid:ApiKey" "YOUR_OWN_SENDGRID_KEY"
   ```

   For a local development admin account, also configure `SeedAdmin:Email` and `SeedAdmin:Password` with unique values in user secrets. The API seeds that account only in Development, when both values are present. Do not reuse a password or put one in the repository.

   Alternatively, set `JwtSettings__Secret`, `SendGrid__ApiKey`, and `ConnectionStrings__DBConnection` as environment variables. Never use the example strings as real credentials.

3. Run the API:

   ```bash
   dotnet restore Internship.Api.sln
   dotnet run --project Internship.Api
   ```

   The application attempts to apply EF Core migrations on startup. Its configured local endpoints are `https://localhost:7266` and `http://localhost:5095`; Swagger is enabled in Development at `/swagger`.

The Angular scaffold is separate from the verified API scope. It should not be cited as an integrated refund/dispute frontend.

## Security and portfolio notes

- The JWT signing secret is supplied outside source control. A value previously committed to the public repository must be treated as exposed and rotated if it was used anywhere beyond local testing. A new commit alone does not remove the old value from Git history.
- Runtime files in `wwwroot/uploads` should not be committed. Previously committed screenshots should be reviewed for personal or company information and removed from history if necessary.
- Uploaded attachments are not served as anonymous static files. Proposed authenticated GET routes at `/api/RefundRequest/{refundId}/attachment` and `/api/Dispute/{disputeId}/attachment` verify resource access and return a download. They require local integration testing, including missing files and different user roles.
- API authorization should be verified with accounts in different roles. Do not use the repository as a production service without a full security review.

## Current limitations

This repository has not been validated by this README edit on a running SQL Server/SendGrid setup. It does not currently include a dedicated automated test project or CI workflow. Add reproducible tests for the ownership and status transitions before advertising it as production-ready.
