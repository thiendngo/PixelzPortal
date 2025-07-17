using Microsoft.Extensions.Logging;
using PixelzPortal.Application.Interfaces;
using PixelzPortal.Application.Results;
using PixelzPortal.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PixelzPortal.Application.Services
{

    public class ProductionService : IProductionService
    {
        private readonly ILogger<ProductionService> _logger;

        public ProductionService(ILogger<ProductionService> logger)
        {
            _logger = logger;
        }

        public Task<ProductionPushResult> PushOrderAsync(Order order)
        {
            // Simulate mock failure if order name contains "fail"
            if (order.Name.ToLower().Contains("fail"))
            {
                _logger.LogWarning("Mock production failure for order {OrderId}", order.Id);
                return Task.FromResult(ProductionPushResult.Fail("Order name triggered production failure."));
            }

            _logger.LogInformation("Mock production push succeeded for order {OrderId}", order.Id);
            return Task.FromResult(ProductionPushResult.Ok());
        }
    }

}
