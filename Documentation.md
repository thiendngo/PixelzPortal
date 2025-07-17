# Pixelz Portal - Application Technical Documentation

## 1. Overview

Pixelz Portal is a full-stack platform designed to help eCommerce studios manage their order workflow, payment processing, invoice creation, and order production integration.

The system includes a .NET 8 backend following SOLID and a few Design Pattern principles, integrated with Kafka for production queue messaging, and an Angular 20 frontend for user interaction.

---

## 2. Features

- User authentication and role-based access (User, ItSupport, Manager)
- Order creation, file attachments, editing, and status tracking
- Payment processing with idempotency key for safety
- Invoice generation
- Integration with mock Production system with Kafka fallback
- Production failure queuing for manager resolution
- RESTful API documentation via Swagger
- Responsive Angular frontend with role-based UI rendering
- Solution is splitted into multiple projects so team members can work on each service separately.
- Data integrity can be achieved by implementing Unit-of-Work design pattern.

---

## 3. Technologies

- **Backend:** ASP.NET Core 8
- **Frontend:** Angular 20
- **Database:** Entity Framework Core with SQL Server
- **Messaging:** Apache Kafka
- **Testing:** NUnit, Moq, EF InMemory
- **Authentication:** ASP.NET Identity with JWT

---

## 4. Backend Architecture

Structured into modular layers:

- **API Layer** (Controllers)
- **Application Layer** (Services, DTOs, Interfaces)
- **Domain Layer** (Entities and Enums)
- **Infrastructure Layer** (EF DbContext, Repositories, Kafka, Email, etc.)
- **Shared Layer** (common utils, Result types)

Controller logic is thin and delegates business logic to service classes.

---

## 5. Data Models

- **AppUser:** extends IdentityUser, includes role
- **Order:** Id, Name, UserId, TotalAmount, Status, CreatedAt
- **Payment:** Id, OrderId, Amount, Method, Status, InitiatedByUserId, CreatedAt
- **Invoice:** Id, OrderId, Amount, CreatedAt
- **OrderAttachment:** AttachmentId, OrderId, FileName, FileType, Data
- **ProductionQueue:** Id, OrderId, Reason, IsResolved, CreatedAt, ResolvedAt

---

## 6. Key Workflows

### Order Checkout:

1. Validate idempotency key  
2. Change status to `Processing`  
3. Process payment via `PaymentService`  
4. If success → Create invoice, mark as `Paid`  
5. Try to push to production (mocked)  
6. If failure → Queue to `ProductionQueue` + Kafka  
7. Send confirmation email  

### Push to Production (Manager only):

1. Validate order is `Paid`  
2. Change status to `InProduction`  
3. Mark queue item as resolved if exists  
4. Send Kafka message  

---

## 7. Frontend Design

- Modular Angular 20 frontend with lazy-loaded features
- Role-based UI rendering
- JWT stored in memory or header
- Dynamic button rendering: Checkout, Push to Production, Save
- File upload using `FormData` and API integration

---

## 8. Design Patterns

### 1. Repository Pattern

- **Purpose:** Abstracts data access logic from business logic  
- **Used In:**  
  - `IOrderRepository`, `IAttachmentRepository`, `IOrderPaymentKeyRepository`, `IUserRepository`  
  - Implemented in `/Infrastructure/Repository/EFOrderRepository.cs`  
- **Benefit:** Easily testable and swappable (in-memory/SQL/mocks)  

### 2. Unit of Work Pattern

- **Purpose:** Wrapping all database changes in a single transaction, so either:
  -	All changes commit together, or
  -	Any failure causes a rollback of everything
 
- **Used In:**  
  - `IUnitOfWork`, implemented in `UnitOfWork.cs`  
- **Benefit:** All-or-nothing DB transactions (e.g., order + attachment + payment)  

### 3. Dependency Injection (DI)

- **Purpose:** Promotes loose coupling and testability  
- **Used In:**  
  - All controllers and services via constructor injection  
  - Configured in `Program.cs`  
- **Benefit:** Swappable implementations for testing/mocking  

### 4. Singleton Pattern

- **Purpose:** Ensures a single instance across app lifecycle  
- **Used In:**  
  - `ILogger<T>` and `IKafkaProducer`  
- **Benefit:** Shared logging and Kafka producer  

---

## 9. Testing

- Unit tests using **NUnit** and **Moq**
- **EF InMemory** used for testing repositories
- Full coverage for `OrderService`: Create, Attach, Checkout, Push

---

## 10. Future Improvements

- Integration Testing
- Admin dashboard for `ProductionQueue` monitoring
- Audit logging and metrics
- Cloud storage for notes
- Virus scanning for files
- Invoice retrieval
- Global API exception handling middleware
- Use CQRS to separate Read/Write
- Explore **SAGA** orchestration
- Offload services like file upload to separate microservices

---

## 11. Assumptions Made

1. **Files stored in SQL Server**  
   - Stored in `varbinary(max)`  
   - Poor for large/high-frequency uploads  

2. **File Upload Limit**  
   - ≤ 10MB per file  

3. **Invoice**  
   - Created for recordkeeping, not delivery  

4. **Production Service**  
   - Marks order as `InProduction` only  

5. **Order Return**  
   - Delivered via separate channels (email/postal/portal)  

6. **Order Payment**  
   - External payments (Wire Transfer) allowed; manually marked as paid  

7. **User Load Expectations**  
   - Local dev: ~10 concurrent users  
   - On-prem SQL: 50–100  
   - With Redis, S3: 300–1000  
   - For 10,000+ users - we need to utilize below to achieve it:  
     - Docker/K8s (ECS/EKS)  
     - Read replicas  
     - CQRS  
     - Kafka-based inter-service messaging  

---

## 12. Time Tracking

### Backend - 6 hours

| Task                                      | Time Spent |
|------------------------------------------|-------------|
| Solution setup, Swagger, EF Core config  | 0.75 hr     |
| Identity, roles, JWT auth                | 0.75 hr     |
| Models (Order, Payment, Invoice, Attach) | 0.75 hr     |
| Checkout API + error handling            | 1.25 hr     |
| Mock Payment/Production, queue fallback  | 1.0 hr      |
| Retry Production (Manager), email notif. | 0.5 hr      |
| Repositories, UoW, seed data             | 1.0 hr      |

### Frontend - 3 hours

| Task                              | Time Spent |
|----------------------------------|-------------|
| Project setup, routing, tokens   | 0.5 hr      |
| Login + role-based navigation    | 0.5 hr      |
| Order creation (User/Manager)    | 0.75 hr     |
| Checkout w/ idempotency key      | 0.5 hr      |
| File upload                      | 0.25 hr     |
| Retry production view (Manager)  | 0.5 hr      |

---

## 13. APIs

### Attachments

| Method | Endpoint                            | Description                   |
|--------|-------------------------------------|-------------------------------|
| GET    | `/api/Attachments/{id}`             | Get attachment metadata by ID |
| DELETE | `/api/Attachments/{id}`             | Delete attachment by ID       |
| GET    | `/api/Attachments/{id}/download`    | Download attachment by ID     |

### Auth

| Method | Endpoint           | Description     |
|--------|--------------------|-----------------|
| POST   | `/api/auth/login`  | User login      |
| POST   | `/api/auth/logout` | User logout     |

### Order

| Method | Endpoint                                 | Description                        |
|--------|------------------------------------------|------------------------------------|
| GET    | `/api/orders/my`                         | Get orders for logged-in user      |
| GET    | `/api/orders/all`                        | Get all orders (Manager/ITSupport) |
| POST   | `/api/orders`                            | Create a new order                 |
| POST   | `/api/orders/{orderId}/attachments`      | Upload attachments to an order     |
| GET    | `/api/orders/{orderId}/attachments`      | Get attachments for an order       |
| PUT    | `/api/orders/{id}`                       | Update an order                    |
| GET    | `/api/orders/{orderId}`                  | Get an order by ID                 |
| POST   | `/api/orders/{id}/checkout`              | Checkout (pay for) an order        |
| POST   | `/api/orders/{id}/production`            | Push a paid order to production    |

### ProductionQueue

| Method | Endpoint                  | Description                  |
|--------|---------------------------|------------------------------|
| GET    | `/api/production-queue`   | Get all production queue items |

### User

| Method | Endpoint       | Description     |
|--------|----------------|-----------------|
| GET    | `/api/users`   | List all users  |
