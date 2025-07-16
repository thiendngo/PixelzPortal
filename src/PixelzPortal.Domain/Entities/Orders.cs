using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PixelzPortal.Domain.Entities
{
    public enum OrderStatus { Created, Processing, Paid, Failed }

    public class Order
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = default!;
        public decimal TotalAmount { get; set; }
        public OrderStatus Status { get; set; } = OrderStatus.Created;
        public string UserId { get; set; } = default!;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Timestamp]
        public byte[] RowVersion { get; set; } = default!;
    }

    public class CreateOrderDto
    {
        public string UserId { get; set; } = default!; // Target user
        public string Name { get; set; } = default!;
        public decimal TotalAmount { get; set; }
        public List<IFormFile>? Attachments { get; set; }
    }

}
