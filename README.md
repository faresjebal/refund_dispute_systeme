# Refund and Dispute Management API

ASP.NET Core internship project built during my placement at MS Solution. The API handles transactions, refund requests, disputes, staff decisions, and audit records. It uses ASP.NET Identity/JWT authentication, Entity Framework Core, and SQL Server.

## Code layout

| Project | Purpose |
| --- | --- |
| `Internship.Api` | REST controllers, authentication, authorization, Swagger, and startup |
| `Internship.Application` | Business services, interfaces, and DTOs |
| `Internship.Domain` | Domain entities and contracts |
| `Internship.Infrastructure` | EF Core persistence, migrations, repositories, and integrations |
| `Internship.Web/ClientApp` | Angular starter scaffold; no verified refund/dispute UI integration |

The work I present from this repository is the **backend API**. The presence of an Angular scaffold does not mean this project has a completed Angular frontend.

## Review status

A [draft security and documentation PR](https://github.com/faresjebal/refund_dispute_systeme/pull/1) proposes ownership/authorization fixes, removal of exposed development credentials and a test email endpoint, and safer upload handling. It has **not** been merged or run against a local SQL Server in this review. The draft now includes authenticated attachment download routes; they need local integration testing before merging.

The public Git history contains an earlier JWT value and development artifacts. Treat any reused secret as exposed and review historical screenshots for sensitive content. This is a portfolio project, not a production deployment. See the draft PR for proposed setup instructions and remaining limitations.
