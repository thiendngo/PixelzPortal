using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PixelzPortal.Domain.Entities
{
    public class OrderAttachment
    {
        public Guid AttachmentId { get; set; } // Primary Key

        public Guid OrderId { get; set; }
        public Order Order { get; set; } = default!;

        public byte[] Data { get; set; } = default!;
        public string FileType { get; set; } = default!;
        public string FileName { get; set; } = default!;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }


}
