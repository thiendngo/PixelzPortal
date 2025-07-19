using PixelzPortal.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PixelzPortal.Application.Interfaces
{
    public interface IAttachmentRepository
    {
        Task<List<OrderAttachment>> GetAttachmentsByOrderIdAsync(Guid orderId);
        Task<OrderAttachment?> GetAttachmentByIdAsync(Guid attachmentId);
        void Add(OrderAttachment attachment);
        Task SaveChangesAsync();

        void Remove(OrderAttachment attachment);
    }
}
