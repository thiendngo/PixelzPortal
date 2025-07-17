# ?? PixelzPortal Architecture Structure

This project follows **Onion Architecture**. Below is the folder and responsibility layout across layers.

---

## ?? Project Structure

```
/PixelzPortal.Domain               # Core business entities and rules
?
??? Entities/                      # Order, Invoice, Payment, etc.
??? Enums/                         # OrderStatus, PaymentMethod, etc.
??? PixelzPortal.Domain.csproj

/PixelzPortal.Application          # Application contracts and logic orchestration
?
??? DTOs/                          # CreateOrderDto, UpdateOrderDto, etc.
??? Interfaces/                    # Repository & service interfaces (IOrderRepository, etc.)
??? Results/                       # PaymentResult, ProductionPushResult, etc.
??? PixelzPortal.Application.csproj

/PixelzPortal.Infrastructure       # Implements application contracts
?
??? Messaging/                     # KafkaProducer, etc.
??? Persistence/                   # AppDbContext, DbSeeder
??? Repository/                    # OrderRepository, PaymentRepository, etc.
??? Services/                      # PaymentService, EmailService, etc.
??? UnitOfWork/                    # UnitOfWork class for transaction control
??? PixelzPortal.Infrastructure.csproj

/PixelzPortal.Api                  # Web API presentation layer
?
??? Controllers/                   # OrderController, AuthController, etc.
??? Properties/
??? appsettings.json
??? Program.cs
??? PixelzPortal.Api.csproj
```

---

## ?? Dependency Flow

```
[API] ? [Application] ? [Domain]
   ?
[Infrastructure (implements Application interfaces)]
```

---
