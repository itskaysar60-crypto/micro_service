# OrderService

ASP.NET Core 6 microservice responsible for creating orders, persisting them locally, and publishing `OrderCreatedEvent` messages to RabbitMQ using the **Transactional Outbox Pattern**.

---

## Architecture

```
Presentation  (ASP.NET Core Web API)
     │
Application   (Business logic, DTOs, Validators, Services)
     │
Infrastructure (EF Core, RabbitMQ Publisher, Outbox Relay, Repositories)
     │
Domain        (Entities, Interfaces, Exceptions — no dependencies)
     │
Shared.Contracts (OrderCreatedEvent — shared with InventoryService)
```

---

## Features

- ✅ Create orders with full validation (FluentValidation)
- ✅ Transactional Outbox Pattern — order + outbox event saved atomically
- ✅ RabbitMQ publisher — outbox relay polls every 10 s and publishes pending events
- ✅ JWT Authentication — stateless, no database required
- ✅ Database transaction (IUnitOfWork) — rollback on failure

---

## NuGet Packages — Project-by-Project

### `Domain` — _no packages_
Pure C# — entities, interfaces, exceptions only.

---

### `Application`

| Package | Version | Purpose | Install Command |
|---|---|---|---|
| `FluentValidation` | 11.9.2 | Validation rules for DTOs | `dotnet add package FluentValidation --version 11.9.2` |
| `FluentValidation.DependencyInjectionExtensions` | 11.9.2 | `AddValidatorsFromAssemblyContaining<T>()` DI | `dotnet add package FluentValidation.DependencyInjectionExtensions --version 11.9.2` |
| `Microsoft.Extensions.DependencyInjection.Abstractions` | 6.0.0 | `IServiceCollection` — keeps Application free of ASP.NET | `dotnet add package Microsoft.Extensions.DependencyInjection.Abstractions --version 6.0.0` |

---

### `Infrastructure`

| Package | Version | Purpose | Install Command |
|---|---|---|---|
| `Microsoft.EntityFrameworkCore.SqlServer` | 6.0.36 | SQL Server database provider | `dotnet add package Microsoft.EntityFrameworkCore.SqlServer --version 6.0.36` |
| `Microsoft.EntityFrameworkCore.Tools` | 6.0.36 | `dotnet ef` migrations (dev-only) | `dotnet add package Microsoft.EntityFrameworkCore.Tools --version 6.0.36` |
| `Microsoft.Extensions.Hosting.Abstractions` | 6.0.0 | `BackgroundService` for `RabbitMqOutboxRelay` | `dotnet add package Microsoft.Extensions.Hosting.Abstractions --version 6.0.0` |
| `Microsoft.Extensions.Http` | 6.0.0 | `IHttpClientFactory` (kept for future use) | `dotnet add package Microsoft.Extensions.Http --version 6.0.0` |
| `RabbitMQ.Client` | 6.8.1 | Publish `OrderCreatedEvent` to RabbitMQ exchange | `dotnet add package RabbitMQ.Client --version 6.8.1` |

---

### `Presentation` (startup / Web API host)

| Package | Version | Purpose | Install Command |
|---|---|---|---|
| `FluentValidation.AspNetCore` | 11.3.0 | Auto 400 on invalid DTOs | `dotnet add package FluentValidation.AspNetCore --version 11.3.0` |
| `Microsoft.AspNetCore.Authentication.JwtBearer` | 6.0.36 | Validate incoming `Bearer` tokens | `dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer --version 6.0.36` |
| `Microsoft.EntityFrameworkCore.Design` | 6.0.36 | EF Core design-time tooling for `dotnet ef` | `dotnet add package Microsoft.EntityFrameworkCore.Design --version 6.0.36` |
| `Swashbuckle.AspNetCore` | 6.2.3 | Swagger UI + OpenAPI spec | `dotnet add package Swashbuckle.AspNetCore --version 6.2.3` |

---

## Configuration — `appsettings.json`

```json
{
  "ConnectionStrings": {
    "OrderDb": "Server=...;Database=OrderDb_Branch01;Trusted_Connection=True;"
  },
  "RabbitMq": {
    "Host": "YOUR_RABBITMQ_SERVER",
    "Port": 5672,
    "Username": "guest",
    "Password": "guest",
    "VirtualHost": "/"
  },
  "Jwt": {
    "SecretKey": "CHANGE_ME_min32chars_secret_key!!",
    "Issuer": "OrderService",
    "Audience": "OrderServiceClients",
    "ExpiryMinutes": 60,
    "AdminUsername": "admin",
    "AdminPassword": "Admin@1234"
  }
}
```

---

## Database Migrations

```bash
# Run from the solution root
dotnet ef migrations add <MigrationName> --project OrderService/Infrastructure --startup-project OrderService/Presentation
dotnet ef database update --project OrderService/Infrastructure --startup-project OrderService/Presentation
```

---

## JWT Authentication

| Endpoint | Method | Auth Required |
|---|---|---|
| `/api/auth/token` | `POST` | ❌ Public |
| `/api/orders` | `GET / POST` | ✅ Bearer token |
| `/api/orders/{id}` | `GET` | ✅ Bearer token |
| `/api/orders/unsynced` | `GET` | ✅ Bearer token |
| `/api/orders/sync-status` | `GET` | ✅ Bearer token |

**Get a token:**
```bash
POST /api/auth/token
Content-Type: application/json

{ "username": "admin", "password": "Admin@1234" }
```

**Use the token:**
```
Authorization: Bearer <token>
```

---

## RabbitMQ Topology (Publisher)

| Item | Value |
|---|---|
| Exchange | `order.created` (direct, durable) |
| Routing Key | `order.created` |
| Message format | JSON — `OrderCreatedEvent` from `Shared.Contracts` |
| Publish timing | Background relay polls every **10 seconds** |
