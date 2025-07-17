using PixelzPortal.Application.Results;
using PixelzPortal.Application.Services;
using PixelzPortal.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PixelzPortal.Application.Interfaces
{
    public interface IProductionService
    {
        Task<ProductionPushResult> PushOrderAsync(Order order);
    }
}
