using PixelzPortal.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PixelzPortal.Application.Interfaces
{
    public interface IOrderPaymentKeyRepository
    {
        Task<bool> IsKeyUsedAsync(Guid orderId, string key);
        Task AddAsync(OrderPaymentKey key);
        Task SaveChangesAsync();
    }
}
