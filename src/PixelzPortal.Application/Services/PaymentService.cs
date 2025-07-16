using Microsoft.Extensions.Logging;
using PixelzPortal.Domain.Entities;
using PixelzPortal.Infrastructure.Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PixelzPortal.Application.Services
{
    public interface IPaymentService
    {
        Task<PaymentResult> ProcessPaymentAsync(Order order, string userId);
    }

    public class PaymentResult
    {
        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }

        public static PaymentResult Success(Guid id) => new() { IsSuccess = true };
        public static PaymentResult Fail(string error) => new() { IsSuccess = false, ErrorMessage = error };
    }

    public class PaymentService : IPaymentService
    {
        private readonly AppDbContext _db;
        private readonly ILogger<PaymentService> _logger;

        public PaymentService(AppDbContext db, ILogger<PaymentService> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<PaymentResult> ProcessPaymentAsync(Order order, string userId)
        {
            // Simulate known failure conditions
            if (order.TotalAmount <= 0)
            {
                _logger.LogWarning("Payment rejected: invalid amount");
                return PaymentResult.Fail("Invalid payment amount.");
            }

            // Simulate external payment gateway delay
            await Task.Delay(500);

            // Mock payment by timestamp: even seconds = success, odd = fail
            int currentSecond = DateTime.UtcNow.Second;

            if (currentSecond % 2 != 0) // odd second = fail
            {
                _logger.LogError("Mocked payment failure for OrderId {OrderId} at second {Second}", order.Id, currentSecond);
                return PaymentResult.Fail("Payment gateway error");
            }


            // Save to Payment table
            var payment = new Payment
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                Method = PaymentMethod.CreditCard, // mock value
                Status = PaymentStatus.Success,
                Amount = order.TotalAmount,
                InitiatedByUserId = userId,
                CreatedAt = DateTime.UtcNow
            };

            _db.Payments.Add(payment);
            await _db.SaveChangesAsync();

            _logger.LogInformation("Payment succeeded for Order {OrderId}, PaymentId {PaymentId}", order.Id, payment.Id);

            return PaymentResult.Success(payment.Id);
        }
    }


}
