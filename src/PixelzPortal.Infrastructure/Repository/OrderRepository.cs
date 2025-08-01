﻿using Microsoft.EntityFrameworkCore;
using PixelzPortal.Application.Interfaces;
using PixelzPortal.Domain.Entities;
using PixelzPortal.Infrastructure.Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PixelzPortal.Infrastructure.Repository
{
    public class OrderRepository : IOrderRepository
    {
        private readonly AppDbContext _context;
        public OrderRepository(AppDbContext context) => _context = context;

        public Task<List<Order>> GetOrdersByUserIdAsync(string userId) =>
            _context.Orders.Where(o => o.UserId == userId).ToListAsync();

        public Task<List<Order>> GetAllOrdersAsync() => _context.Orders.ToListAsync();

        public Task<Order?> GetByIdAsync(Guid id) =>
            _context.Orders.FirstOrDefaultAsync(o => o.Id == id);

        public Task<bool> OrderExistsAsync(Guid id) =>
            _context.Orders.AnyAsync(o => o.Id == id);

        public void AddAttachment(OrderAttachment attachment) =>
            _context.OrderAttachments.Add(attachment);

        public Task<List<OrderAttachment>> GetAttachmentsByOrderIdAsync(Guid orderId) =>
            _context.OrderAttachments.Where(a => a.OrderId == orderId).ToListAsync();

        public void SaveIdempotencyKey(OrderPaymentKey key) =>
            _context.OrderPaymentKeys.Add(key);

        public Task<bool> IdempotencyKeyExistsAsync(Guid orderId, string key) =>
            _context.OrderPaymentKeys.AnyAsync(k => k.OrderId == orderId && k.Key == key);

        public async Task AddOrderAsync(Order order)
        {
            _context.Orders.Add(order);
        }

        public async Task AddInvoiceAsync(Invoice invoice)
        {
            _context.Invoices.Add(invoice);
        }
        public async Task<Order?> GetOrderByIdAsync(Guid orderId)
        {
            return await _context.Orders.FirstOrDefaultAsync(o => o.Id == orderId);
        }

        public Task SaveChangesAsync() => _context.SaveChangesAsync();

        public async Task QueueProductionFailureAsync(ProductionQueue queueItem)
        {
            await _context.ProductionQueue.AddAsync(queueItem);
        }

        public async Task<ProductionQueue?> GetActiveQueueByOrderIdAsync(Guid orderId)
        {
            return await _context.ProductionQueue
                .FirstOrDefaultAsync(q => q.OrderId == orderId && !q.IsResolved);
        }

        public async Task AddPaymentAsync(Payment payment)
        {
            await _context.Payments.AddAsync(payment);
        }

    }
}
