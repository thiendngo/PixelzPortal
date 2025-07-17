using PixelzPortal.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PixelzPortal.Application.Interfaces
{
    public interface IAttachmentService
    {
        Task<OrderAttachment?> GetAttachmentAsync(Guid id);

        Task<List<OrderAttachment>> GetAllAttachmentsByOrderIdAsync(Guid orderId);

        Task<object?> GetAttachmentMetadataAsync(Guid id);
        Task<bool> DeleteAttachmentAsync(Guid id);
    }
}
