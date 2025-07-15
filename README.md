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

### 3. Run the application
- User list is seeded.

- A few orders are added.

---

## A few issues:
