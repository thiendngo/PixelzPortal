using Microsoft.Extensions.Logging;
using PixelzPortal.Application.Interfaces;
using PixelzPortal.Application.Results;
using PixelzPortal.Domain.Entities;
using PixelzPortal.Domain.Enums;

namespace PixelzPortal.Application.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly IPaymentRepository _payments;
        private readonly ILogger<PaymentService> _logger;

        public PaymentService(IPaymentRepository payments, ILogger<PaymentService> logger)
        {
            _payments = payments;
            _logger = logger;
        }

        public async Task<PaymentResult> ProcessPaymentAsync(Order order, string userId)
        {
            if (order.TotalAmount <= 0)
            {
                _logger.LogWarning("Payment rejected: invalid amount");
                return PaymentResult.Fail("Invalid payment amount.");
            }

            await Task.Delay(500);

            int currentSecond = DateTime.UtcNow.Second;
            if (currentSecond % 2 != 0)
            {
                _logger.LogError("Mocked payment failure for OrderId {OrderId} at second {Second}", order.Id, currentSecond);
                return PaymentResult.Fail("Payment gateway error");
            }

            var payment = new Payment
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                Method = PaymentMethod.CreditCard,
                Status = PaymentStatus.Success,
                Amount = order.TotalAmount,
                InitiatedByUserId = userId,
                CreatedAt = DateTime.UtcNow
            };

            await _payments.AddAsync(payment);
            await _payments.SaveChangesAsync();

            _logger.LogInformation("Payment succeeded for Order {OrderId}, PaymentId {PaymentId}", order.Id, payment.Id);

            return PaymentResult.Success(payment.Id);
        }
    }



}
