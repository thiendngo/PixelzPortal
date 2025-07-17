using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using PixelzPortal.Application.Services;
using PixelzPortal.Domain.Entities;
using PixelzPortal.Domain.Enums;
using PixelzPortal.Infrastructure.Persistence;
using PixelzPortal.Infrastructure.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PixelzPortal.Tests.Services
{
    [TestFixture]
    public class PaymentServiceTests
    {
        private AppDbContext _db = null!;
        private PaymentService _service = null!;

        [SetUp]
        public void SetUp()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _db = new AppDbContext(options);

            var paymentRepository = new PaymentRepository(_db); // ✅ Correct type
            var loggerMock = new Mock<ILogger<PaymentService>>();

            _service = new PaymentService(paymentRepository, loggerMock.Object); // ✅ No longer passes _db
        }


        [TearDown]
        public void TearDown()
        {
            _db.Dispose();
        }

        [Test]
        public async Task ProcessPaymentAsync_ValidOrder_AddsPaymentToDatabase_OnSuccess()
        {
            // Arrange
            var order = new Order
            {
                Id = Guid.NewGuid(),
                Name = "Test Order",
                TotalAmount = 100,
                UserId = "user-1"
            };

            // Act
            var result = await _service.ProcessPaymentAsync(order, "user-1");

            // Assert
            if (result.IsSuccess)
            {
                var saved = await _db.Payments.FirstOrDefaultAsync(p => p.OrderId == order.Id);
                Assert.That(saved, Is.Not.Null);
                Assert.That(saved!.Amount, Is.EqualTo(100));
                Assert.That(saved.Status, Is.EqualTo(PaymentStatus.Success));
            }
            else
            {
                Assert.That(result.ErrorMessage, Is.EqualTo("Payment gateway error"));
            }
        }

        [Test]
        public async Task ProcessPaymentAsync_ZeroAmount_FailsImmediately()
        {
            // Arrange
            var order = new Order
            {
                Id = Guid.NewGuid(),
                Name = "Free Order",
                TotalAmount = 0,
                UserId = "user-2"
            };

            // Act
            var result = await _service.ProcessPaymentAsync(order, "user-2");

            // Assert
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.ErrorMessage, Is.EqualTo("Invalid payment amount."));
        }
    }
}
