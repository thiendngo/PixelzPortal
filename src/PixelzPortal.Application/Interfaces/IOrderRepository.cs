using PixelzPortal.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PixelzPortal.Application.Interfaces
{
    public interface IOrderRepository
    {
        Task<List<Order>> GetOrdersByUserIdAsync(string userId);
        Task<List<Order>> GetAllOrdersAsync();
        Task<Order?> GetByIdAsync(Guid id);
        Task<bool> OrderExistsAsync(Guid id);
        Task SaveChangesAsync();
        void AddAttachment(OrderAttachment attachment);
        Task<List<OrderAttachment>> GetAttachmentsByOrderIdAsync(Guid orderId);
        Task<Order?> GetOrderByIdAsync(Guid orderId);

        Task<bool> IdempotencyKeyExistsAsync(Guid orderId, string key);
        void SaveIdempotencyKey(OrderPaymentKey key);
        Task AddOrderAsync(Order order);
        Task AddInvoiceAsync(Invoice invoice);

        Task QueueProductionFailureAsync(ProductionQueue queueItem);
        Task<ProductionQueue?> GetActiveQueueByOrderIdAsync(Guid orderId);
        Task AddPaymentAsync(Payment payment);

    }
}
