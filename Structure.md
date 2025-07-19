# PixelzPortal Architecture Structure

This project follows **Onion Architecture**. Below is the folder and responsibility layout across layers.

---

##  Project Structure

```
src/
│
├── PixelzPortal.Api              # Presentation Layer
│   ├── Controllers
│   │   ├── AttachmentController.cs
│   │   ├── AuthController.cs
│   │   ├── OrderController.cs
│   │   ├── ProductionQueueController.cs
│   │   └── UserController.cs
│   └── ...
│
├── PixelzPortal.Application      # Application Layer
│   ├── DTOs
│   │   ├── CreateOrderDto.cs
│   │   └── UpdateOrderDto.cs
│   ├── Interfaces
│   │   ├── IAttachmentService.cs
│   │   ├── IEmailService.cs
│   │   ├── IOrderService.cs
│   │   ├── IPaymentService.cs
│   │   └── IProductionService.cs
│   ├── Results
│   │   ├── PaymentResult.cs
│   │   └── ProductionPushResult.cs
│   └── Services
│       ├── AttachmentService.cs
│       ├── EmailService.cs
│       ├── OrderService.cs
│       ├── PaymentService.cs
│       └── ProductionService.cs
│
├── PixelzPortal.Domain           # Domain Layer
│   ├── Entities
│   │   ├── AppUser.cs
│   │   ├── Invoices.cs
│   │   ├── OrderAttachment.cs
│   │   ├── OrderPaymentKey.cs
│   │   ├── Orders.cs
│   │   ├── Payments.cs
│   │   └── ProductionQueue.cs
│   ├── Enums
│   │   ├── OrderStatus.cs
│   │   ├── PaymentMethod.cs
│   │   └── PaymentStatus.cs
│   └── Interfaces
│       ├── IAttachmentRepository.cs
│       ├── IKafkaProducer.cs
│       ├── IOrderPaymentKeyRepository.cs
│       ├── IOrderRepository.cs
│       ├── IPaymentRepository.cs
│       ├── IProductionQueueRepository.cs
│       └── IUnitOfWork.cs
│
├── PixelzPortal.Infrastructure   # Infrastructure Layer
│   ├── Messaging
│   │   └── KafkaProducer.cs
│   ├── Persistence
│   │   ├── AppDbContext.cs
│   │   └── DbSeeder.cs
│   ├── Repository
│   │   ├── AttachmentRepository.cs
│   │   ├── OrderPaymentKeyRepository.cs
│   │   ├── OrderRepository.cs
│   │   ├── PaymentRepository.cs
│   │   └── ProductionQueueRepository.cs
│   └── UnitOfWork
│       └── UnitOfWork.cs

```

---

## Dependency Flow

```
[PixelzPortal.Api (Presentation/WebApi)]
            ↓
[PixelzPortal.Application (Application Layer)]
            ↓
[PixelzPortal.Domain (Domain Layer)]
            ↑
[PixelzPortal.Infrastructure (Infrastructure Layer)]

```

---
