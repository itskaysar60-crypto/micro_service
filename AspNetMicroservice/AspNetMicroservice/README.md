# ERP Microservices — ASP.NET Core

Offline-first ERP billing system using **Clean Architecture**, **SOLID principles**, and the **Transactional Outbox Pattern** with **RabbitMQ** as the message transport.

---

## Solution Structure

```
AspNetMicroservice\
├── OrderService/                  ← Branch/Local service (creates bills, publishes events)
│   ├── Domain/                    ← Entities, Interfaces, Exceptions (zero dependencies)
│   ├── Application/               ← Business logic, DTOs, FluentValidation, Service interfaces
│   ├── Infrastructure/            ← EF Core, Repositories, RabbitMqPublisher, RabbitMqOutboxRelay
│   │   ├── Messaging/             ← RabbitMqPublisher, RabbitMqSettings
│   │   ├── Sync/                  ← RabbitMqOutboxRelay (BackgroundService), HttpOutboxRelay (legacy)
│   │   ├── Persistence/           ← OrderDbContext
│   │   └── Repositories/         ← OrderRepository, OutboxRepository
│   └── Presentation/              ← Web API, Program.cs, JWT setup
│       ├── Controllers/           ← OrdersController, AuthController
│       └── Auth/                  ← JwtSettings
│
├── InventoryService/              ← Central/Cloud service (manages stock, consumes events)
│   ├── Domain/
│   ├── Application/
│   ├── Infrastructure/
│   │   ├── Messaging/             ← RabbitMqOrderConsumer (BackgroundService), RabbitMqSettings
│   │   ├── Persistence/           ← InventoryDbContext
│   │   └── Repositories/         ← ProductRepository, ProcessedEventRepository
│   └── Presentation/
│       └── Controllers/           ← ProductsController, SyncController
│
└── Shared/
    └── Shared.Contracts/
        └── Events/                ← OrderCreatedEvent (shared DTO between services)
```

---

## Prerequisites

- **.NET 6 SDK**
- **SQL Server** or **SQL Server LocalDB** (ships with Visual Studio)
- **RabbitMQ** — running locally on `localhost:5672` with default credentials (`guest` / `guest`)
  - Quickest option: `docker run -d -p 5672:5672 -p 15672:15672 rabbitmq:3-management`
  - Management UI: http://localhost:15672 (guest/guest)

---

## How to Run

### 1. Apply EF Core Migrations

Run from the **solution root** (`AspNetMicroservice\`):

```bash
# OrderService
dotnet ef migrations add InitialCreate \
  --project OrderService/Infrastructure \
  --startup-project OrderService/Presentation
dotnet ef database update \
  --project OrderService/Infrastructure \
  --startup-project OrderService/Presentation

# InventoryService
dotnet ef migrations add InitialCreate \
  --project InventoryService/Infrastructure \
  --startup-project InventoryService/Presentation
dotnet ef database update \
  --project InventoryService/Infrastructure \
  --startup-project InventoryService/Presentation
```

### 2. Start Both Services

```bash
# Terminal 1 — InventoryService (port 5002) — start first so the queue is ready
cd InventoryService\Presentation
dotnet run

# Terminal 2 — OrderService (port 5001)
cd OrderService\Presentation
dotnet run
```

### 3. Obtain a JWT Token (OrderService is protected)

```bash
POST http://localhost:5001/api/auth/token
Content-Type: application/json

{ "username": "admin", "password": "Admin@1234" }
```

Response:
```json
{ "token": "eyJ...", "expiresAt": "...", "type": "Bearer" }
```

Use the token on all subsequent OrderService requests:
```
Authorization: Bearer <token>
```

### 4. Test the Full Flow

```bash
# Step 1 — Create a product with stock (InventoryService — no auth)
curl -X POST http://localhost:5002/api/products \
  -H "Content-Type: application/json" \
  -d '{"name":"Widget A","sku":"WDG-001","stockQuantity":100}'

# Step 2 — Create an order (OrderService — requires Bearer token)
curl -X POST http://localhost:5001/api/orders \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer <token>" \
  -d '{"branchId":"Branch01","customerName":"John","items":[{"productId":"<PRODUCT_ID>","quantity":5,"unitPrice":10.00}]}'

# Step 3 — Check sync status (within ~10 s the relay publishes to RabbitMQ)
curl http://localhost:5001/api/orders/sync-status \
  -H "Authorization: Bearer <token>"

# Step 4 — Verify stock was deducted (should show 95)
curl http://localhost:5002/api/products/<PRODUCT_ID>/stock
```

---

## Messaging Architecture — RabbitMQ

```
OrderService                          RabbitMQ Broker                  InventoryService
─────────────                         ──────────────                   ────────────────
User creates order
  ↓
Single DB transaction:
  ✅ Order  →  Orders table
  ✅ Event  →  OutboxEvents table (IsPublished = false)
  ↓
User gets 201 immediately
  ↓
RabbitMqOutboxRelay (every 10 s):
  → Poll OutboxEvents WHERE IsPublished = false
  → Publish JSON to exchange ──────────────────────────────→  Exchange: order.created
  → Mark event Published                                       Routing key: order.created
  → Mark Order.IsSynced = true                                Queue: order.created.inventory
                                                               ↓
                                                         RabbitMqOrderConsumer:
                                                           → Deserialise OrderCreatedEvent
                                                           → Check ProcessedEvents (idempotency)
                                                           → Deduct stock
                                                           → Save ProcessedEvent
                                                           → BasicAck (manual ack)
```

### RabbitMQ Topology

| Item | OrderService (Publisher) | InventoryService (Consumer) |
|---|---|---|
| Exchange | `order.created` — direct, durable | same |
| Routing Key | `order.created` | `order.created` |
| Queue | — | `order.created.inventory` — durable |
| Message format | JSON `OrderCreatedEvent` | JSON `OrderCreatedEvent` |
| Ack mode | n/a | Manual (`autoAck: false`) |
| Prefetch | n/a | 1 |

---

## Configuration

### OrderService — `appsettings.json`

```json
{
  "ConnectionStrings": {
    "OrderDb": "Server=(localdb)\\mssqllocaldb;Database=OrderDb_Branch01;Trusted_Connection=True;"
  },
  "BranchId": "Branch01",
  "RabbitMq": {
    "Host": "localhost",
    "Port": 5672,
    "Username": "guest",
    "Password": "guest",
    "VirtualHost": "/"
  },
  "Jwt": {
    "SecretKey": "OrderService_SuperSecret_Key_Min32Chars!",
    "Issuer": "OrderService",
    "Audience": "OrderServiceClients",
    "ExpiryMinutes": 60,
    "AdminUsername": "admin",
    "AdminPassword": "Admin@1234"
  }
}
```

### InventoryService — `appsettings.json`

```json
{
  "ConnectionStrings": {
    "InventoryDb": "Server=(localdb)\\mssqllocaldb;Database=InventoryDb;Trusted_Connection=True;"
  },
  "RabbitMq": {
    "Host": "localhost",
    "Port": 5672,
    "Username": "guest",
    "Password": "guest",
    "VirtualHost": "/"
  }
}
```

---

## API Endpoints

### OrderService — http://localhost:5001

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| `POST` | `/api/auth/token` | ❌ Public | Exchange credentials for a JWT token |
| `POST` | `/api/orders` | ✅ Bearer | Create a bill/order |
| `GET`  | `/api/orders` | ✅ Bearer | List all orders |
| `GET`  | `/api/orders/{id}` | ✅ Bearer | Get order by ID |
| `GET`  | `/api/orders/unsynced` | ✅ Bearer | Orders pending RabbitMQ publish |
| `GET`  | `/api/orders/sync-status` | ✅ Bearer | Published vs pending counts |

### InventoryService — http://localhost:5002

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| `POST` | `/api/products` | ❌ Public | Create product with initial stock |
| `GET`  | `/api/products` | ❌ Public | List all products |
| `GET`  | `/api/products/{id}/stock` | ❌ Public | Get stock level |
| `POST` | `/api/sync/orders` | ❌ Public | HTTP fallback sync (backward compat) |

---

## SOLID Principles Applied

| Principle | Implementation |
|-----------|---------------|
| **S** — Single Responsibility | Each class has one job: Controller, Service, Repository, Publisher |
| **O** — Open/Closed | New transport targets (e.g. Azure Service Bus) extend without modifying existing relay |
| **L** — Liskov Substitution | Any `IOrderRepository` implementation is substitutable |
| **I** — Interface Segregation | `IOrderRepository` and `IOutboxRepository` are separate contracts |
| **D** — Dependency Inversion | Controllers → `IOrderService`, Services → `IRepository`, Relay → `IOutboxRepository` |

---

## Design Decisions

| Decision | Reason |
|----------|--------|
| **RabbitMQ over HttpClient** | Decoupled, durable, broker-guaranteed delivery; services need not be simultaneously online |
| **Transactional Outbox Pattern** | Order + outbox event written in one DB transaction — no lost events on crash |
| **Idempotent consumer** | `ProcessedEvents` table deduplicates by `EventId` — safe to retry |
| **Manual RabbitMQ ack** | Message stays in queue until processing fully succeeds — no silent data loss |
| **JWT — no DB** | Credentials stored in `appsettings.json`; no user-management overhead for internal service auth |
| **Separate DBs** | Each service owns its data — true microservice boundary |
| **No MediatR** | Plain service interfaces are simpler and SOLID-compliant without extra dependencies |
| **`IServiceScopeFactory` in BackgroundService** | Singleton relay needs Scoped `DbContext` — scope created per poll cycle |
| **`ProductId` as GUID reference** | No cross-DB foreign keys; only GUID references between services |
