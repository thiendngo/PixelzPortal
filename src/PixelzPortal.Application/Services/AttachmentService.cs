using Microsoft.EntityFrameworkCore;
using PixelzPortal.Domain.Entities;
using PixelzPortal.Infrastructure.Persistence;
using PixelzPortal.Infrastructure.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PixelzPortal.Application.Services
{
    public interface IAttachmentService
    {
        Task<OrderAttachment?> GetAttachmentAsync(Guid id);

        Task<List<OrderAttachment>?> GetAllAttachmentsByOrderIdAsync(Guid orderId);

        Task<object?> GetAttachmentMetadataAsync(Guid id);
        Task<bool> DeleteAttachmentAsync(Guid id);
    }


    public class AttachmentService : IAttachmentService
    {
        private readonly IAttachmentRepository _attachmentRepository;

        public AttachmentService(IAttachmentRepository attachmentRepository)
        {
            _attachmentRepository = attachmentRepository;
        }

        public async Task<OrderAttachment?> GetAttachmentAsync(Guid id)
        {
            return await _attachmentRepository.GetAttachmentByIdAsync(id);
        }

        public async Task<object?> GetAttachmentMetadataAsync(Guid id)
        {
            var attachment = await _attachmentRepository.GetAttachmentByIdAsync(id);
            if (attachment == null) return null;

            return new
            {
                attachment.AttachmentId,
                attachment.FileName,
                attachment.FileType,
                attachment.CreatedAt,
                attachment.OrderId
            };
        }
        public async Task<List<OrderAttachment>> GetAllAttachmentsByOrderIdAsync(Guid orderId)
        {
            return await _attachmentRepository.GetAttachmentsByOrderIdAsync(orderId);
        }

        public async Task<bool> DeleteAttachmentAsync(Guid id)
        {
            var attachment = await _attachmentRepository.GetAttachmentByIdAsync(id);
            if (attachment == null) return false;

            _attachmentRepository.Remove(attachment);
            await _attachmentRepository.SaveChangesAsync();
            return true;
        }

    }
}
