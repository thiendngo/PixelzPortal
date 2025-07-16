using PixelzPortal.Domain.Entities;
using PixelzPortal.Infrastructure.Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PixelzPortal.Tests.Utils
{
    public static class TestDataSeeder
    {
        public static void SeedOrders(AppDbContext context, string userId, int total = 5)
        {
            for (int i = 0; i < total; i++)
            {
                context.Orders.Add(new Order
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    Name = $"Order {i + 1}",
                    CreatedAt = DateTime.UtcNow.AddMinutes(-i),
                    Status = i % 4 == 0 ? OrderStatus.Paid : OrderStatus.Created,
                    TotalAmount = 100 + i * 5
                });
            }

            context.SaveChanges();
        }

        public static void SeedProductionQueue(AppDbContext context, Guid orderId)
        {
            context.ProductionQueue.Add(new ProductionQueue
            {
                Id = Guid.NewGuid(),
                OrderId = orderId,
                Reason = "Test Failure",
                CreatedAt = DateTime.UtcNow,
                IsResolved = false
            });

            context.SaveChanges();
        }
    }
}
