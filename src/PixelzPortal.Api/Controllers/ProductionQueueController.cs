using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PixelzPortal.Application.Interfaces;
using PixelzPortal.Infrastructure.Repository;

namespace PixelzPortal.Api.Controllers
{
    [ApiController]
    [Route("api/production-queue")]
    [Authorize(Roles = "Manager")]
    public class ProductionQueueController : ControllerBase
    {
        private readonly IProductionQueueRepository _queueRepo;

        public ProductionQueueController(IProductionQueueRepository queueRepo)
        {
            _queueRepo = queueRepo;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllUnresolved()
        {
            var items = await _queueRepo.GetAllUnresolvedAsync();

            var result = items.Select(q => new
            {
                q.Id,
                q.OrderId,
                q.Reason,
                q.CreatedAt,
                q.IsResolved
            });

            return Ok(result);
        }
    }
}
