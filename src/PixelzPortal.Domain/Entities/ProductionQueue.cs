using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PixelzPortal.Domain.Entities
{
    public class ProductionQueue
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid OrderId { get; set; } // Foreign key only

        public string Reason { get; set; } = default!;
        public bool IsResolved { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ResolvedAt { get; set; }
    }


}
