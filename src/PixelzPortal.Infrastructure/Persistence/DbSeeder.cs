using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using PixelzPortal.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PixelzPortal.Infrastructure.Persistence
{
    public static class DbSeeder
    {
        public static async Task SeedAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            string[] roles = ["User", "ItSupport", "Manager"];
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                    await roleManager.CreateAsync(new IdentityRole(role));
            }

            // Create users
            var it = new AppUser { UserName = "it@example.com", Email = "it@example.com", EmailConfirmed = true };
            var manager = new AppUser { UserName = "manager@example.com", Email = "manager@example.com", EmailConfirmed = true };


            if (await userManager.FindByEmailAsync(it.Email) is null)
            {
                await userManager.CreateAsync(it, "It@123");
                await userManager.AddToRoleAsync(it, "ItSupport");
            }

            if (await userManager.FindByEmailAsync(manager.Email) is null)
            {
                await userManager.CreateAsync(manager, "Manager@123");
                await userManager.AddToRoleAsync(manager, "Manager");
            }

            // Seed orders
            // ===== SEED REGULAR USERS WITH ORDERS =====
            var rnd = new Random();
            var users = new List<AppUser>();

            for (int i = 1; i <= 5; i++)
            {
                var email = $"user{i}@example.com";
                var user = new AppUser { UserName = email, Email = email, EmailConfirmed = true };

                if (await userManager.FindByEmailAsync(email) == null)
                {
                    await userManager.CreateAsync(user, "User@123");
                    await userManager.AddToRoleAsync(user, "User");
                    users.Add(user);
                }
                else
                {
                    users.Add(await userManager.FindByEmailAsync(email));
                }
            }

            if (!db.Orders.Any())
            {
                var orderList = new List<Order>();

                foreach (var user in users)
                {
                    int orderCount = rnd.Next(3, 11); // Between 3 and 10
                    for (int i = 0; i < orderCount; i++)
                    {
                        bool isPaid = rnd.NextDouble() < 0.2; // 20% chance
                        orderList.Add(new Order
                        {
                            Id = Guid.NewGuid(),
                            Name = $"{user.UserName}-Order-{i + 1}",
                            TotalAmount = rnd.Next(50, 300),
                            Status = isPaid ? OrderStatus.Paid : OrderStatus.Created,
                            UserId = user.Id,
                            CreatedAt = DateTime.UtcNow.AddDays(-rnd.Next(1, 30))
                        });
                    }
                }

                db.Orders.AddRange(orderList);
                await db.SaveChangesAsync();
            }
        }
    }

}
