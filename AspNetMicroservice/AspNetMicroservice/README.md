# ERP Microservices — ASP.NET Core

Offline-first ERP billing system using **Clean Architecture**, **SOLID principles**, and **Transactional Outbox Pattern**.

## Architecture

```
D:\AspNetMicroservice\
├── OrderService/           ← Branch/Local service (creates bills)
│   ├── Domain/             ← Entities, Interfaces (zero dependencies)
│   ├── Application/        ← Business logic, DTOs, Service interfaces
│   ├── Infrastructure/     ← EF Core, Repositories, HttpOutboxRelay
│   └── Presentation/       ← Web API controllers, Program.cs
│
├── InventoryService/       ← Central/Cloud service (manages stock)
│   ├── Domain/
│   ├── Application/
│   ├── Infrastructure/
│   └── Presentation/
│
└── Shared/
    └── Shared.Contracts/   ← Event DTOs shared between services
```

## How to Run

### Prerequisites
- .NET 6 SDK
- SQL Server or SQL Server LocalDB (comes with Visual Studio)

### 1. Create databases (EF Core migrations)

```bash
# OrderService
cd D:\AspNetMicroservice\OrderService\Presentation
dotnet ef migrations add InitialCreate --project ../Infrastructure
dotnet ef database update

# InventoryService
cd D:\AspNetMicroservice\InventoryService\Presentation
dotnet ef migrations add InitialCreate --project ../Infrastructure
dotnet ef database update
```

### 2. Run both services

```bash
# Terminal 1 — InventoryService (port 5002) — start this first
cd D:\AspNetMicroservice\InventoryService\Presentation
dotnet run

# Terminal 2 — OrderService (port 5001)
cd D:\AspNetMicroservice\OrderService\Presentation
dotnet run
```

### 3. Test the flow

```bash
# Step 1: Create a product with stock
curl -X POST http://localhost:5002/api/products \
  -H "Content-Type: application/json" \
  -d '{"name":"Widget A","sku":"WDG-001","stockQuantity":100}'

# Step 2: Create an order (use the productId from step 1)
curl -X POST http://localhost:5001/api/orders \
  -H "Content-Type: application/json" \
  -d '{"branchId":"Branch01","customerName":"John","items":[{"productId":"<PRODUCT_ID>","quantity":5,"unitPrice":10.00}]}'

# Step 3: Check sync status
curl http://localhost:5001/api/orders/sync-status

# Step 4: Check stock (should be reduced by 5)
curl http://localhost:5002/api/products/<PRODUCT_ID>/stock
```

## Offline Sync Strategy

### Transactional Outbox Pattern

```
User creates bill (offline or online)
    ↓
Single DB transaction:
  ✅ Order saved to Orders table
  ✅ OutboxEvent saved to OutboxEvents table (IsPublished = false)
    ↓
User gets response immediately — no internet dependency
    ↓
HttpOutboxRelay (Background Service, every 10 seconds):
  → Polls OutboxEvents WHERE IsPublished = false
  → Internet available?
      YES → POST to InventoryService /api/sync/orders → mark Published
      NO  → Retry silently, increment RetryCount
    ↓
InventoryService receives event:
  → Check ProcessedEvents (idempotency)
  → Already processed? → Skip
  → New? → Deduct stock → Record in ProcessedEvents
```

### Why This Pattern?

| Problem | Solution |
|---------|----------|
| Order saved but sync fails | Outbox guarantees event is saved atomically with order |
| Duplicate events | ProcessedEvents table deduplicates by EventId |
| Service is down | RetryCount with max 10 attempts, retries every 10s |
| No internet | Bills work offline, sync happens when connection restores |

## Design Decisions

| Decision | Reason |
|----------|--------|
| No MediatR | Plain service interfaces are simpler, SOLID-compliant without extra library |
| No RabbitMQ | HttpClient-based sync — no Docker dependency needed |
| SQL Server | Production-ready, LocalDB for development |
| Separate DBs | Microservice principle — each service owns its data |
| ProductId as reference | No cross-DB foreign keys — only GUID references |
| IServiceScopeFactory in BackgroundService | Singleton service needs Scoped DbContext |

## SOLID Principles Applied

| Principle | Implementation |
|-----------|---------------|
| **S** — Single Responsibility | Each class has one job (Controller, Service, Repository) |
| **O** — Open/Closed | New sync targets extend without modifying existing code |
| **L** — Liskov Substitution | Any IOrderRepository implementation works |
| **I** — Interface Segregation | IOrderRepository and IOutboxRepository are separate |
| **D** — Dependency Inversion | Controllers → IOrderService, Services → IRepository |

## API Endpoints

### OrderService — http://localhost:5001
| Method | Route | Description |
|--------|-------|-------------|
| POST | /api/orders | Create bill |
| GET | /api/orders | List all orders |
| GET | /api/orders/{id} | Get order detail |
| GET | /api/orders/unsynced | Pending sync queue |
| GET | /api/orders/sync-status | Sync statistics |

### InventoryService — http://localhost:5002
| Method | Route | Description |
|--------|-------|-------------|
| POST | /api/products | Create product with stock |
| GET | /api/products | List all products |
| GET | /api/products/{id}/stock | Check stock level |
| POST | /api/sync/orders | Receive synced order (internal) |
