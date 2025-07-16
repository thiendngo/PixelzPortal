using Microsoft.AspNetCore.Authorization;
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

        public OrderController(
            IOrderRepository orders,
            IAttachmentRepository attachments,
            IPaymentService paymentService,
            IOrderPaymentKeyRepository paymentKeys,
            IUnitOfWork unitOfWork)
        {
            _orders = orders;
            _attachments = attachments;
            _paymentService = paymentService;
            _paymentKeys = paymentKeys;
            _unitOfWork = unitOfWork;
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
                return BadRequest("No attachments uploaded.");

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
            if (order == null) return NotFound();

            bool keyExists = await _paymentKeys.IsKeyUsedAsync(id, idempotencyKey);
            if (keyExists)
                return Conflict("Duplicate payment request detected.");

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                var usedKey = new OrderPaymentKey
                {
                    Id = Guid.NewGuid(),
                    OrderId = id,
                    Key = idempotencyKey,
                    CreatedAt = DateTime.UtcNow
                };
                await _paymentKeys.AddAsync(usedKey);

                if (order.Status != OrderStatus.Created && order.Status != OrderStatus.Failed)
                    return BadRequest("Order is already paid or being processed.");

                order.Status = OrderStatus.Processing;
                await _orders.SaveChangesAsync();

                var paymentResult = await _paymentService.ProcessPaymentAsync(order, order.UserId);
                if (!paymentResult.IsSuccess)
                {
                    order.Status = OrderStatus.Failed;
                    await _orders.SaveChangesAsync();
                    await _unitOfWork.CommitAsync();
                    return BadRequest($"Payment failed: {paymentResult.ErrorMessage}");
                }

                var invoice = new Invoice
                {
                    Id = Guid.NewGuid(),
                    OrderId = order.Id,
                    Amount = order.TotalAmount,
                    CreatedAt = DateTime.UtcNow
                };

                await _orders.AddInvoiceAsync(invoice);
                order.Status = OrderStatus.Paid;
                await _orders.SaveChangesAsync();

                await _unitOfWork.CommitAsync();
                return Ok(new { message = "Order checked out and pushed to production." });
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                return StatusCode(500, $"Unexpected error: {ex.Message}");
            }
        }



    }

}
