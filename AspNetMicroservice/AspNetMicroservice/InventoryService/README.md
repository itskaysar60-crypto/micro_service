# InventoryService

ASP.NET Core 6 microservice responsible for managing product stock. Consumes `OrderCreatedEvent` messages from RabbitMQ and deducts inventory accordingly.

---

## Architecture

```
Presentation  (ASP.NET Core Web API)
     │
Application   (Business logic, DTOs, Services)
     │
Infrastructure (EF Core, RabbitMQ Consumer, Repositories)
     │
Domain        (Entities, Interfaces, Exceptions — no dependencies)
     │
Shared.Contracts (OrderCreatedEvent — shared with OrderService)
```

---

## Features

- ✅ Consume `OrderCreatedEvent` from RabbitMQ queue
- ✅ Idempotent processing — skips duplicate events using `ProcessedEvent` table
- ✅ Stock deduction per order item
- ✅ Manual message acknowledgment — no data loss on crash
- ✅ REST API to manage products and check stock

---

## NuGet Packages — Project-by-Project

### `Domain` — _no packages_
Pure C# — entities, interfaces, exceptions only.

---

### `Application`

| Package | Version | Purpose | Install Command |
|---|---|---|---|
| `Microsoft.Extensions.DependencyInjection.Abstractions` | 6.0.0 | `IServiceCollection` — keeps Application free of ASP.NET | `dotnet add package Microsoft.Extensions.DependencyInjection.Abstractions --version 6.0.0` |
| `Microsoft.Extensions.Logging.Abstractions` | 6.0.4 | `ILogger<T>` interface for service logging | `dotnet add package Microsoft.Extensions.Logging.Abstractions --version 6.0.4` |

---

### `Infrastructure`

| Package | Version | Purpose | Install Command |
|---|---|---|---|
| `Microsoft.EntityFrameworkCore.SqlServer` | 6.0.36 | SQL Server database provider | `dotnet add package Microsoft.EntityFrameworkCore.SqlServer --version 6.0.36` |
| `Microsoft.EntityFrameworkCore.Tools` | 6.0.36 | `dotnet ef` migrations (dev-only) | `dotnet add package Microsoft.EntityFrameworkCore.Tools --version 6.0.36` |
| `Microsoft.Extensions.Hosting.Abstractions` | 6.0.0 | `BackgroundService` for `RabbitMqOrderConsumer` | `dotnet add package Microsoft.Extensions.Hosting.Abstractions --version 6.0.0` |
| `RabbitMQ.Client` | 6.8.1 | Consume `OrderCreatedEvent` from RabbitMQ queue | `dotnet add package RabbitMQ.Client --version 6.8.1` |

---

### `Presentation` (startup / Web API host)

| Package | Version | Purpose | Install Command |
|---|---|---|---|
| `Microsoft.EntityFrameworkCore.Design` | 6.0.36 | EF Core design-time tooling for `dotnet ef` | `dotnet add package Microsoft.EntityFrameworkCore.Design --version 6.0.36` |
| `Swashbuckle.AspNetCore` | 6.2.3 | Swagger UI + OpenAPI spec | `dotnet add package Swashbuckle.AspNetCore --version 6.2.3` |

---

## Configuration — `appsettings.json`

```json
{
  "ConnectionStrings": {
    "InventoryDb": "Server=...;Database=InventoryDb;Trusted_Connection=True;"
  },
  "RabbitMq": {
    "Host": "YOUR_RABBITMQ_SERVER",
    "Port": 5672,
    "Username": "guest",
    "Password": "guest",
    "VirtualHost": "/"
  }
}
```

> ⚠️ **`RabbitMq:Host` must point to the same server as OrderService.**

---

## Database Migrations

```bash
# Run from the solution root
dotnet ef migrations add <MigrationName> --project InventoryService/Infrastructure --startup-project InventoryService/Presentation
dotnet ef database update --project InventoryService/Infrastructure --startup-project InventoryService/Presentation
```

---

## REST API Endpoints

| Endpoint | Method | Description |
|---|---|---|
| `/api/products` | `GET` | List all products |
| `/api/products` | `POST` | Create a product |
| `/api/products/{id}/stock` | `GET` | Get stock info for a product |
| `/api/sync/orders` | `POST` | HTTP fallback — receive order sync (kept for backward compat) |

---

## RabbitMQ Topology (Consumer)

| Item | Value |
|---|---|
| Exchange | `order.created` (direct, durable) |
| Queue | `order.created.inventory` (durable) |
| Routing Key | `order.created` |
| Ack mode | Manual (`autoAck: false`) — acks only after successful processing |
| Prefetch | 1 — processes one message at a time |
| Message format | JSON — `OrderCreatedEvent` from `Shared.Contracts` |

### Consumer flow per message

```
1. Deserialise JSON → OrderCreatedEvent
2. Check ProcessedEvent table (idempotency)
3. Deduct stock for each OrderItem
4. Save ProcessedEvent record
5. BasicAck → message removed from queue
   (on error → BasicNack, no requeue)
```
