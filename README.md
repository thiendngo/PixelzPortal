﻿# Pixelz Portal

A Web API for managing orders and payments.

---

## Features

- Role-based authentication (User, IT, Manager)
---

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
- [SQL Server](https://www.microsoft.com/en-us/sql-server) (localdb or full instance)
- [EF Core CLI tools](https://learn.microsoft.com/en-us/ef/core/cli/dotnet) (`dotnet ef`)

---

## Getting Started

### 1. Setup SQL Database
- After creating the DB instance, make sure you change the ConnectionString.
``` 
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost\\SQLEXPRESS;Database=PixelzPortalDb;MultipleActiveResultSets=true;TrustServerCertificate=True;"
}
```

### 2. Run EF Core Migration
```
dotnet ef migrations add InitialSetup --project src/PixelzPortal.Infrastructure --startup-project src/PixelzPortal.Api
dotnet ef database update --project src/PixelzPortal.Infrastructure --startup-project src/PixelzPortal.Api

```
- Check if the PixelzPortalDb database is created in SQL instance..

### 2. Setup Kafka
- Install Java JDK 12+
- Download Kafka Scala binary (2.13) at https://kafka.apache.org/downloads
- Extract the folder to C:\ (C:\Kafka)
- Use PowerShell, navigate to C:\Kafka
```
.\bin\windows\zookeeper-server-start.bat .\config\zookeeper.properties
$env:KAFKA_HEAP_OPTS = "-Xmx1G -Xms1G"
.\bin\windows\kafka-server-start.bat .\config\server.properties

```
- This will setup Kafka.
- Kafka is used in very minor feature to pop notification if an order is paid but failed to push to production (InProduction status)

### 3. Run the application
- User list is seeded.
- Users: User1@example.com : User1@123
  - Similar with User2,3,4,5, etc.
- ITSupport: it@example.com : it@123
- Manager: manager@example.com : manager@123
---

