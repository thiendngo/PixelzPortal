using Microsoft.AspNetCore.Http;
using PixelzPortal.Application.DTOs;
using PixelzPortal.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace PixelzPortal.Application.Interfaces
{
    public interface IOrderService
    {
        Task<IEnumerable<Order>> GetOrdersByUserIdAsync(string userId);
        Task<IEnumerable<Order>> GetAllOrdersAsync();
        Task<Order> CreateOrderAsync(CreateOrderDto dto, string currentUserId, ClaimsPrincipal user);
        Task UploadAttachmentsAsync(Guid orderId, List<IFormFile> attachments);
        Task<Order?> GetOrderByIdAsync(Guid orderId, ClaimsPrincipal user);
        Task UpdateOrderAmountAsync(Guid orderId, decimal totalAmount);
        Task<(bool Success, string? Error)> CheckoutOrderAsync(Guid orderId, string idempotencyKey);
        Task<(bool Success, string? Error)> PushToProductionAsync(Guid orderId);
        Task TestQueueInsertAsync();
        Task<(bool Success, string? Error)> MarkOrderAsPaidAsync(Guid orderId, string initiatedByUserId);

    }
}
