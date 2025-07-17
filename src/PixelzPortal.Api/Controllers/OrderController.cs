using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PixelzPortal.Application.DTOs;
using PixelzPortal.Application.Interfaces;
using PixelzPortal.Application.Services;

using System.Security.Claims;

namespace PixelzPortal.Api.Controllers
{



    [ApiController]
    [Authorize]
    [Route("api/orders")]
    public class OrderController : ControllerBase
    {
        private readonly IOrderService _orderService;
        private readonly IAttachmentService _attachmentService;

        public OrderController(IOrderService orderService,
            IAttachmentService attachmentService)
        {
            _orderService = orderService;
            _attachmentService = attachmentService;
        }

        [HttpGet("my")]
        public async Task<IActionResult> MyOrders()
        {
            var userId = User.FindFirstValue("userId");
            var orders = await _orderService.GetOrdersByUserIdAsync(userId);
            return Ok(orders);
        }

        [HttpGet("all")]
        [Authorize(Roles = "ItSupport,Manager")]
        public async Task<IActionResult> AllOrders()
        {
            var orders = await _orderService.GetAllOrdersAsync();
            return Ok(orders);
        }

        [HttpPost]
        public async Task<IActionResult> CreateOrder([FromForm] CreateOrderDto dto)
        {
            var currentUserId = User.FindFirstValue("userId");
            if (currentUserId == null) return Unauthorized();

            try
            {
                var created = await _orderService.CreateOrderAsync(dto, currentUserId, User);
                return CreatedAtAction(nameof(MyOrders), new { id = created.Id }, created);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
        }

        [HttpPost("{orderId}/attachments")]
        public async Task<IActionResult> UploadAttachments(Guid orderId, [FromForm] List<IFormFile> attachments)
        {
            if (attachments == null || attachments.Count == 0)
                return Ok();

            await _orderService.UploadAttachmentsAsync(orderId, attachments);
            return Ok();
        }

        [HttpGet("{orderId}")]
        public async Task<IActionResult> GetOrderById(Guid orderId)
        {
            var result = await _orderService.GetOrderByIdAsync(orderId, User);
            return result == null ? Forbid() : Ok(result);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateOrder(Guid id, [FromBody] UpdateOrderDto dto)
        {
            await _orderService.UpdateOrderAmountAsync(id, dto.TotalAmount);
            return NoContent();
        }

        [HttpPost("{id}/checkout")]
        public async Task<IActionResult> CheckoutOrder(Guid id)
        {
            var key = Request.Headers["Idempotency-Key"].ToString();
            if (string.IsNullOrWhiteSpace(key)) return BadRequest("Missing Idempotency-Key");

            var result = await _orderService.CheckoutOrderAsync(id, key);
            return result.Success ? Ok(new { message = "Order checked out." }) : StatusCode(502, result.Error);
        }

        [HttpPost("{id}/production")]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> PushToProduction(Guid id)
        {
            var result = await _orderService.PushToProductionAsync(id);
            return result.Success ? Ok(new { message = "Order pushed to production." }) : StatusCode(400, result.Error);
        }

        [HttpPost("test-queue")]
        public async Task<IActionResult> TestQueue()
        {
            await _orderService.TestQueueInsertAsync();
            return Ok("Inserted and published.");
        }

        [HttpPost("{id}/mark-paid")]
        [Authorize(Roles = "ItSupport,Manager")]
        public async Task<IActionResult> MarkAsPaid(Guid id)
        {
            var userId = User.FindFirstValue("userId");
            if (userId == null) return Unauthorized();

            var result = await _orderService.MarkOrderAsPaidAsync(id, userId);

            if (!result.Success)
                return BadRequest(result.Error);

            return Ok(new { message = "Order manually marked as paid." });
        }

        [HttpGet("{orderId}/attachments")]
        public async Task<IActionResult> GetOrderAttachments(Guid orderId)
        {

            var order = await _orderService.GetOrderByIdAsync(orderId, User);
            if (order == null)
                return NotFound();

            var attachments = await _attachmentService.GetAllAttachmentsByOrderIdAsync(orderId);

            var result = attachments.Select(a => new
            {
                attachmentId = a.AttachmentId,
                fileName = a.FileName,
                fileType = a.FileType,
                createdAt = a.CreatedAt
            });

            return Ok(result);
        }

    }
}
