using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using PixelzPortal.Domain.Entities;
using PixelzPortal.Infrastructure.Messaging;
using PixelzPortal.Infrastructure.Repository;
using PixelzPortal.Infrastructure.UnitOfWork;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace PixelzPortal.Application.Services
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
    }

    public class OrderService : IOrderService
    {
        private readonly IOrderRepository _orders;
        private readonly IAttachmentRepository _attachments;
        private readonly IPaymentService _paymentService;
        private readonly IOrderPaymentKeyRepository _paymentKeys;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEmailService _email;
        private readonly UserManager<AppUser> _userManager;
        private readonly IProductionService _productionService;
        private readonly IKafkaProducer _kafka;

        public OrderService(
            IOrderRepository orders,
            IAttachmentRepository attachments,
            IPaymentService paymentService,
            IOrderPaymentKeyRepository paymentKeys,
            IUnitOfWork unitOfWork,
            IEmailService email,
            UserManager<AppUser> userManager,
            IProductionService productionService,
            IKafkaProducer kafka)
        {
            _orders = orders;
            _attachments = attachments;
            _paymentService = paymentService;
            _paymentKeys = paymentKeys;
            _unitOfWork = unitOfWork;
            _email = email;
            _userManager = userManager;
            _productionService = productionService;
            _kafka = kafka;
        }

        public async Task<IEnumerable<Order>> GetOrdersByUserIdAsync(string userId)
            => await _orders.GetOrdersByUserIdAsync(userId);

        public async Task<IEnumerable<Order>> GetAllOrdersAsync()
            => await _orders.GetAllOrdersAsync();

        public async Task<Order> CreateOrderAsync(CreateOrderDto dto, string currentUserId, ClaimsPrincipal userClaims)
        {
            if (dto.UserId != currentUserId && !userClaims.IsInRole("ItSupport") && !userClaims.IsInRole("Manager"))
                throw new UnauthorizedAccessException("Only ITSupport or Manager can create orders for others.");

            await _unitOfWork.BeginTransactionAsync();

            var order = new Order
            {
                Id = Guid.NewGuid(),
                Name = dto.Name,
                TotalAmount = dto.TotalAmount,
                Status = OrderStatus.Created,
                UserId = dto.UserId
            };

            await _orders.AddOrderAsync(order);

            if (dto.Attachments != null)
            {
                foreach (var file in dto.Attachments)
                {
                    using var ms = new MemoryStream();
                    await file.CopyToAsync(ms);
                    _attachments.Add(new OrderAttachment
                    {
                        AttachmentId = Guid.NewGuid(),
                        OrderId = order.Id,
                        FileName = file.FileName,
                        FileType = file.ContentType,
                        Data = ms.ToArray(),
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }

            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitAsync();
            return order;
        }

        public async Task UploadAttachmentsAsync(Guid orderId, List<IFormFile> attachments)
        {
            foreach (var file in attachments)
            {
                using var ms = new MemoryStream();
                await file.CopyToAsync(ms);
                _attachments.Add(new OrderAttachment
                {
                    AttachmentId = Guid.NewGuid(),
                    OrderId = orderId,
                    FileName = file.FileName,
                    FileType = file.ContentType,
                    Data = ms.ToArray(),
                    CreatedAt = DateTime.UtcNow
                });
            }
            await _orders.SaveChangesAsync();
        }

        public async Task<Order?> GetOrderByIdAsync(Guid orderId, ClaimsPrincipal user)
        {
            var order = await _orders.GetOrderByIdAsync(orderId);
            var userId = user.FindFirstValue("userId");
            if (order == null || (order.UserId != userId && !user.IsInRole("ItSupport") && !user.IsInRole("Manager")))
                return null;
            return order;
        }

        public async Task UpdateOrderAmountAsync(Guid id, decimal totalAmount)
        {
            var order = await _orders.GetOrderByIdAsync(id);
            if (order == null) return;
            order.TotalAmount = totalAmount;
            await _orders.SaveChangesAsync();
        }

        public async Task<(bool Success, string? Error)> CheckoutOrderAsync(Guid orderId, string idempotencyKey)
        {
            var order = await _orders.GetOrderByIdAsync(orderId);
            if (order == null) return (false, "Order not found");

            if (order.Status != OrderStatus.Created && order.Status != OrderStatus.Failed)
                return (false, "Order already paid or processing");

            if (await _paymentKeys.IsKeyUsedAsync(orderId, idempotencyKey))
                return (false, "Duplicate payment");

            await _unitOfWork.BeginTransactionAsync();
            await _paymentKeys.AddAsync(new OrderPaymentKey
            {
                Id = Guid.NewGuid(),
                OrderId = orderId,
                Key = idempotencyKey,
                CreatedAt = DateTime.UtcNow
            });

            order.Status = OrderStatus.Processing;
            await _orders.SaveChangesAsync();

            var result = await _paymentService.ProcessPaymentAsync(order, order.UserId);
            if (!result.IsSuccess)
            {
                order.Status = OrderStatus.Failed;
                await _orders.SaveChangesAsync();
                await _unitOfWork.CommitAsync();
                return (false, result.ErrorMessage);
            }

            await _orders.AddInvoiceAsync(new Invoice
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                Amount = order.TotalAmount,
                CreatedAt = DateTime.UtcNow
            });

            order.Status = OrderStatus.Paid;
            await _orders.SaveChangesAsync();

            var prodResult = await _productionService.PushOrderAsync(order);
            if (!prodResult.Success)
            {
                await _orders.QueueProductionFailureAsync(new ProductionQueue
                {
                    Id = Guid.NewGuid(),
                    OrderId = order.Id,
                    Reason = prodResult.Error ?? "Unknown"
                });
                await _unitOfWork.CommitAsync();
                await _kafka.ProduceAsync("production-queue", JsonSerializer.Serialize(new
                {
                    orderId = order.Id,
                    order.Name,
                    order.UserId,
                    order.CreatedAt
                }));
                return (false, "Payment succeeded but production failed");
            }

            order.Status = OrderStatus.InProduction;
            await _orders.SaveChangesAsync();
            await _unitOfWork.CommitAsync();

            var user = await _userManager.FindByIdAsync(order.UserId);
            if (!string.IsNullOrWhiteSpace(user?.Email))
            {
                await _email.SendOrderConfirmationAsync(user.Email, order.Name, order.TotalAmount);
            }

            return (true, null);
        }

        public async Task<(bool Success, string? Error)> PushToProductionAsync(Guid id)
        {
            var order = await _orders.GetOrderByIdAsync(id);
            if (order == null) return (false, "Not found");
            if (order.Status != OrderStatus.Paid) return (false, "Invalid status");

            order.Status = OrderStatus.InProduction;
            await _orders.SaveChangesAsync();

            var queue = await _orders.GetActiveQueueByOrderIdAsync(order.Id);
            if (queue != null)
            {
                queue.IsResolved = true;
                queue.ResolvedAt = DateTime.UtcNow;
                await _orders.SaveChangesAsync();
            }

            await _kafka.ProduceAsync("production-queue", JsonSerializer.Serialize(new
            {
                orderId = order.Id,
                order.Name,
                order.UserId,
                order.CreatedAt
            }));

            return (true, null);
        }

        public async Task TestQueueInsertAsync()
        {
            var queueItem = new ProductionQueue
            {
                Id = Guid.NewGuid(),
                OrderId = Guid.NewGuid(),
                Reason = "Manual test",
                CreatedAt = DateTime.UtcNow,
                IsResolved = false
            };

            await _orders.QueueProductionFailureAsync(queueItem);
            await _unitOfWork.CommitAsync();

            var kafkaMessage = new
            {
                QueueId = queueItem.Id,
                queueItem.OrderId,
                queueItem.Reason,
                Timestamp = DateTime.UtcNow,
                Source = "TestQueue"
            };
            await _kafka.ProduceAsync("production-queue", kafkaMessage);
        }

    }
}
