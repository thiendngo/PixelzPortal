
using PixelzPortal.Application.Interfaces;
using PixelzPortal.Domain.Entities;

namespace PixelzPortal.Application.Services
{

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
