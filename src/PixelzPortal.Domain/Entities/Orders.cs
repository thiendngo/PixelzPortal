﻿using Microsoft.AspNetCore.Http;
using PixelzPortal.Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PixelzPortal.Domain.Entities
{

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

    
    
    

}
