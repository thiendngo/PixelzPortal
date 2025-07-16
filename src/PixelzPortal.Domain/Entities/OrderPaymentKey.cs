using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PixelzPortal.Domain.Entities
{
    public class OrderPaymentKey
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid OrderId { get; set; }
        public string Key { get; set; } = default!;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

}
