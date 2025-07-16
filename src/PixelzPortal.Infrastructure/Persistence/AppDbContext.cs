using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PixelzPortal.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace PixelzPortal.Infrastructure.Persistence
{
    public class AppDbContext : IdentityDbContext<AppUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Order> Orders => Set<Order>();
        public DbSet<Payment> Payments => Set<Payment>();
        public DbSet<Invoice> Invoices => Set<Invoice>();
        public DbSet<OrderAttachment> OrderAttachments => Set<OrderAttachment>();
        public DbSet<OrderPaymentKey> OrderPaymentKeys => Set<OrderPaymentKey>();
        public DbSet<ProductionQueue> ProductionQueue => Set<ProductionQueue>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Order>().HasIndex(o => o.Name);
            builder.Entity<Invoice>()
                .HasOne(i => i.Order)
                .WithMany()
                .HasForeignKey(i => i.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Payment>()
                .HasOne(p => p.Order)
                .WithMany()
                .HasForeignKey(p => p.OrderId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<OrderAttachment>()
                .HasKey(a => a.AttachmentId);
            builder.Entity<OrderAttachment>()
                .HasOne(a => a.Order)
                .WithMany()
                .HasForeignKey(a => a.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<OrderAttachment>()
                .Property(a => a.Data)
                .HasColumnType("varbinary(max)");

            builder.Entity<OrderPaymentKey>()
                .HasKey(p => p.Id);

            builder.Entity<OrderPaymentKey>()
                .HasIndex(p => new { p.OrderId, p.Key }) // Optional uniqueness
                .IsUnique();

            builder.Entity<ProductionQueue>()
                .HasOne(q => q.Order)
                .WithMany()
                .HasForeignKey(q => q.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
