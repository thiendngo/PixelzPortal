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
            var user = new AppUser { UserName = "user1@example.com", Email = "user1@example.com", EmailConfirmed = true };
            var it = new AppUser { UserName = "it@example.com", Email = "it@example.com", EmailConfirmed = true };
            var manager = new AppUser { UserName = "manager@example.com", Email = "manager@example.com", EmailConfirmed = true };

            if (await userManager.FindByEmailAsync(user.Email) is null)
            {
                await userManager.CreateAsync(user, "User@123");
                await userManager.AddToRoleAsync(user, "User");
            }

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
            if (!db.Orders.Any())
            {
                db.Orders.AddRange(
                    new Order
                    {
                        Id = Guid.NewGuid(),
                        Name = "Unpaid Order",
                        TotalAmount = 100,
                        Status = OrderStatus.Created,
                        UserId = user.Id
                    },
                    new Order
                    {
                        Id = Guid.NewGuid(),
                        Name = "Paid Order",
                        TotalAmount = 200,
                        Status = OrderStatus.Paid,
                        UserId = user.Id
                    }
                );

                await db.SaveChangesAsync();
            }
        }
    }

}
