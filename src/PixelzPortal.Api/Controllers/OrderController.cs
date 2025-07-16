using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PixelzPortal.Application.Services;
using PixelzPortal.Domain.Entities;
using PixelzPortal.Infrastructure.Persistence;
using PixelzPortal.Infrastructure.Repository;
using PixelzPortal.Infrastructure.UnitOfWork;
using System.Security.Claims;

namespace PixelzPortal.Api.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/orders")]
    public class OrderController : ControllerBase
    {
        private readonly IOrderRepository _orders;
        private readonly IAttachmentRepository _attachments;
        private readonly IPaymentService _paymentService;
        private readonly IOrderPaymentKeyRepository _paymentKeys;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEmailService _email;
        private readonly UserManager<AppUser> _userManager;
        private readonly IProductionService _productionService;

        public OrderController(
            IOrderRepository orders,
            IAttachmentRepository attachments,
            IPaymentService paymentService,
            IOrderPaymentKeyRepository paymentKeys,
            IUnitOfWork unitOfWork,
            IEmailService email,
            UserManager<AppUser> userManager,
            IProductionService productionService)
        {
            _orders = orders;
            _attachments = attachments;
            _paymentService = paymentService;
            _paymentKeys = paymentKeys;
            _unitOfWork = unitOfWork;
            _email = email;
            _userManager = userManager;
            _productionService = productionService;
        }




        [HttpGet("my")]
        public async Task<IActionResult> MyOrders()
        {
            var userId = User.FindFirstValue("userId");
            var orders = await _orders.GetOrdersByUserIdAsync(userId);


            return Ok(orders);
        }

        [HttpGet("all")]
        [Authorize(Roles = "ItSupport,Manager")]
        public async Task<IActionResult> AllOrders()
        {
            var orders = await _orders.GetAllOrdersAsync();
            return Ok(orders);
        }

        [HttpPost]
        public async Task<IActionResult> CreateOrder([FromForm] CreateOrderDto dto)
        {
            var currentUserId = User.FindFirstValue("userId");
            if (currentUserId == null) return Unauthorized();

            if (dto.UserId != currentUserId && !User.IsInRole("ItSupport") && !User.IsInRole("Manager"))
                return Forbid("Only ITSupport or Manager can create orders for other users.");

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                var order = new Order
                {
                    Id = Guid.NewGuid(),
                    Name = dto.Name,
                    TotalAmount = dto.TotalAmount,
                    Status = OrderStatus.Created,
                    UserId = dto.UserId
                };

                await _orders.AddOrderAsync(order);

                if (dto.Attachments != null && dto.Attachments.Any())
                {
                    foreach (var file in dto.Attachments)
                    {
                        using var ms = new MemoryStream();
                        await file.CopyToAsync(ms);

                        var attachment = new OrderAttachment
                        {
                            AttachmentId = Guid.NewGuid(),
                            OrderId = order.Id,
                            Data = ms.ToArray(),
                            FileName = file.FileName,
                            FileType = file.ContentType,
                            CreatedAt = DateTime.UtcNow
                        };

                        _attachments.Add(attachment);
                    }
                }

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitAsync();

                return CreatedAtAction(nameof(MyOrders), new { id = order.Id }, order);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                return StatusCode(500, $"Order creation failed: {ex.Message}");
            }
        }



        [HttpPost("{orderId}/attachments")]
        public async Task<IActionResult> UploadAttachments(Guid orderId, [FromForm] List<IFormFile> attachments)
        {
            if (attachments == null || attachments.Count == 0)
                return Ok();

            var orderExists = await _orders.OrderExistsAsync(orderId);
            if (!orderExists)
                return NotFound("Order not found.");

            foreach (var file in attachments)
            {
                if (file.Length == 0) continue;

                using var memoryStream = new MemoryStream();
                await file.CopyToAsync(memoryStream);

                var attachment = new OrderAttachment
                {
                    AttachmentId = Guid.NewGuid(),
                    OrderId = orderId,
                    FileName = file.FileName,
                    FileType = file.ContentType,
                    Data = memoryStream.ToArray(),
                    CreatedAt = DateTime.UtcNow
                };

                _attachments.Add(attachment);
            }

            await _orders.SaveChangesAsync();
            return Ok();
        }


        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> UpdateOrder(Guid id, [FromBody] UpdateOrderDto dto)
        {
            var order = await _orders.GetOrderByIdAsync(id);
            if (order == null)
                return NotFound();

            // Update fields
            order.TotalAmount = dto.TotalAmount;

            await _orders.SaveChangesAsync();
            return NoContent();
        }




        [HttpGet("{orderId}/attachments")]
        public async Task<IActionResult> GetAttachments(Guid orderId)
        {
            var attachments = await _attachments.GetAttachmentsByOrderIdAsync(orderId);

            return Ok(attachments);
        }

        [HttpGet("{orderId}")]
        public async Task<IActionResult> GetOrderById(Guid orderId)
        {
            var currentUserId = User.FindFirstValue("userId"); // or ClaimTypes.NameIdentifier
            if (string.IsNullOrEmpty(currentUserId)) return Unauthorized();

            var order = await _orders.GetOrderByIdAsync(orderId);

            if (order == null)
                return NotFound();

            // If user is not owner and not admin
            if (order.UserId != currentUserId && !User.IsInRole("ItSupport") && !User.IsInRole("Manager"))
                return Forbid("You do not have access to this order.");

            return Ok(order);
        }

        [HttpPost("{id}/checkout")]
        [Authorize]
        public async Task<IActionResult> CheckoutOrder(Guid id)
        {
            var idempotencyKey = Request.Headers["Idempotency-Key"].ToString();
            if (string.IsNullOrWhiteSpace(idempotencyKey))
                return BadRequest("Missing Idempotency-Key header.");

            var order = await _orders.GetOrderByIdAsync(id);
            if (order == null)
                return NotFound();

            bool keyExists = await _paymentKeys.IsKeyUsedAsync(id, idempotencyKey);
            if (keyExists)
                return Conflict("Duplicate payment request detected.");

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                // Record idempotency key
                await _paymentKeys.AddAsync(new OrderPaymentKey
                {
                    Id = Guid.NewGuid(),
                    OrderId = id,
                    Key = idempotencyKey,
                    CreatedAt = DateTime.UtcNow
                });

                // Ensure order is valid for payment
                if (order.Status != OrderStatus.Created && order.Status != OrderStatus.Failed)
                    return BadRequest("Order is already paid or being processed.");

                // Mark order as processing
                order.Status = OrderStatus.Processing;
                await _orders.SaveChangesAsync();

                // Process payment
                var paymentResult = await _paymentService.ProcessPaymentAsync(order, order.UserId);
                if (!paymentResult.IsSuccess)
                {
                    order.Status = OrderStatus.Failed;
                    await _orders.SaveChangesAsync();
                    await _unitOfWork.CommitAsync();
                    return BadRequest($"Payment failed: {paymentResult.ErrorMessage}");
                }

                // Create invoice + mark as paid
                await _orders.AddInvoiceAsync(new Invoice
                {
                    Id = Guid.NewGuid(),
                    OrderId = order.Id,
                    Amount = order.TotalAmount,
                    CreatedAt = DateTime.UtcNow
                });

                order.Status = OrderStatus.Paid;
                await _orders.SaveChangesAsync();

                // Attempt to push to production
                var productionResult = await _productionService.PushOrderAsync(order);

                if (!productionResult.Success)
                {
                    // Push failed → queue it for manager review
                    await _orders.QueueProductionFailureAsync(new ProductionQueue
                    {
                        Id = Guid.NewGuid(),
                        OrderId = order.Id,
                        Reason = productionResult.Error ?? "Unknown error"
                    });

                    await _unitOfWork.CommitAsync();
                    return StatusCode(502, $"Payment succeeded but production failed. Order queued for manager review.");
                }

                // Success: mark order as InProduction
                order.Status = OrderStatus.InProduction;
                await _orders.SaveChangesAsync();
                await _unitOfWork.CommitAsync();

                // Email confirmation
                var user = await _userManager.FindByIdAsync(order.UserId);
                if (!string.IsNullOrWhiteSpace(user?.Email))
                {
                    await _email.SendOrderConfirmationAsync(
                        email: user.Email,
                        orderName: order.Name,
                        amount: order.TotalAmount
                    );
                }

                return Ok(new { message = "Order checked out and sent to production." });
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                return StatusCode(500, $"Unexpected error: {ex.Message}");
            }
        }

        [HttpPost("{id}/production")]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> PushToProduction(Guid id)
        {
            var order = await _orders.GetOrderByIdAsync(id);
            if (order == null)
                return NotFound();

            if (order.Status != OrderStatus.Paid)
                return BadRequest("Only orders in Paid status can be pushed to production.");

            var result = await _productionService.PushOrderAsync(order);

            if (!result.Success)
            {
                // Optionally add to queue again if not already queued
                var existingQueueItem = await _orders.GetActiveQueueByOrderIdAsync(order.Id);
                if (existingQueueItem == null)
                {
                    await _orders.QueueProductionFailureAsync(new ProductionQueue
                    {
                        Id = Guid.NewGuid(),
                        OrderId = order.Id,
                        Reason = result.Error ?? "Unknown failure",
                        CreatedAt = DateTime.UtcNow,
                        IsResolved = false
                    });

                    await _orders.SaveChangesAsync();
                }

                return StatusCode(502, $"Push failed: {result.Error}");
            }

            // ✅ Push succeeded → update order status
            order.Status = OrderStatus.InProduction;
            await _orders.SaveChangesAsync();

            // ✅ Mark queue as resolved (if exists)
            var queueItem = await _orders.GetActiveQueueByOrderIdAsync(order.Id);
            if (queueItem != null)
            {
                queueItem.IsResolved = true;
                queueItem.ResolvedAt = DateTime.UtcNow;
                await _orders.SaveChangesAsync();
            }

            return Ok(new { message = "Order successfully pushed to production." });
        }


    }
}
