using Microsoft.EntityFrameworkCore;
using PixelzPortal.Domain.Entities;
using PixelzPortal.Infrastructure.Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PixelzPortal.Infrastructure.Repository
{
    public interface IOrderPaymentKeyRepository
    {
        Task<bool> IsKeyUsedAsync(Guid orderId, string key);
        Task AddAsync(OrderPaymentKey key);
        Task SaveChangesAsync();
    }

    public class OrderPaymentKeyRepository : IOrderPaymentKeyRepository
    {
        private readonly AppDbContext _db;

        public OrderPaymentKeyRepository(AppDbContext db)
        {
            _db = db;
        }

        public async Task<bool> IsKeyUsedAsync(Guid orderId, string key)
        {
            return await _db.OrderPaymentKeys.AnyAsync(k => k.OrderId == orderId && k.Key == key);
        }

        public async Task AddAsync(OrderPaymentKey key)
        {
            _db.OrderPaymentKeys.Add(key);
            await _db.SaveChangesAsync();
        }

        public async Task SaveChangesAsync()
        {
            await _db.SaveChangesAsync();
        }
    }
}
