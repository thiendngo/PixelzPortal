using Microsoft.EntityFrameworkCore;
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
    
    public class AttachmentRepository : IAttachmentRepository
    {
        private readonly AppDbContext _db;

        public AttachmentRepository(AppDbContext db)
        {
            _db = db;
        }

        public void Add(OrderAttachment attachment)
        {
            _db.OrderAttachments.Add(attachment);
        }

        public async Task<List<OrderAttachment>> GetAttachmentsByOrderIdAsync(Guid orderId)
        {
            return await _db.OrderAttachments
                .Where(a => a.OrderId == orderId)
                .ToListAsync();
        }

        public async Task<OrderAttachment?> GetAttachmentByIdAsync(Guid attachmentId)
        {
            return await _db.OrderAttachments
                .FirstOrDefaultAsync(a => a.AttachmentId == attachmentId);
        }

        public async Task SaveChangesAsync()
        {
            await _db.SaveChangesAsync();
        }

        public void Remove(OrderAttachment attachment)
        {
            _db.OrderAttachments.Remove(attachment);
        }
    }
}
