using PixelzPortal.Application.Results;
using PixelzPortal.Domain.Entities;


namespace PixelzPortal.Application.Interfaces
{
    public interface IProductionService
    {
        Task<ProductionPushResult> PushOrderAsync(Order order);
    }
}
