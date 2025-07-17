using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PixelzPortal.Application.DTOs
{
    public class CreateOrderDto
    {
        public string UserId { get; set; } = default!; // Target user
        public string Name { get; set; } = default!;
        public decimal TotalAmount { get; set; }
        public List<IFormFile>? Attachments { get; set; }
    }
}
