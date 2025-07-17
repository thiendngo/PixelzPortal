using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;
using PixelzPortal.Application.Interfaces;
using PixelzPortal.Application.Services;
using PixelzPortal.Domain.Entities;
using PixelzPortal.Domain.Enums;
using PixelzPortal.Infrastructure.Messaging;
using PixelzPortal.Infrastructure.Persistence;
using PixelzPortal.Infrastructure.Repository;
using PixelzPortal.Infrastructure.UnitOfWork;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace PixelzPortal.Tests.Services
{
    [TestFixture]
    public class OrderServiceTests
    {
        private AppDbContext _db = null!;
        private OrderService _service = null!;
        private Mock<IOrderRepository> _orderRepo = null!;
        private Mock<IAttachmentRepository> _attachmentRepo = null!;
        private Mock<IPaymentService> _paymentService = null!;
        private Mock<IOrderPaymentKeyRepository> _paymentKeys = null!;
        private Mock<IUnitOfWork> _unitOfWork = null!;
        private Mock<IEmailService> _emailService = null!;
        private Mock<IProductionService> _productionService = null!;
        private Mock<IKafkaProducer> _kafkaProducer = null!;
        private Mock<UserManager<AppUser>> _userManager = null!;

        [SetUp]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: "OrderServiceTestDb")
                .Options;

            _db = new AppDbContext(options);
            _orderRepo = new Mock<IOrderRepository>();
            _attachmentRepo = new Mock<IAttachmentRepository>();
            _paymentService = new Mock<IPaymentService>();
            _paymentKeys = new Mock<IOrderPaymentKeyRepository>();
            _unitOfWork = new Mock<IUnitOfWork>();
            _emailService = new Mock<IEmailService>();
            _productionService = new Mock<IProductionService>();
            _kafkaProducer = new Mock<IKafkaProducer>();

            var userStore = new Mock<IUserStore<AppUser>>();
            _userManager = new Mock<UserManager<AppUser>>(userStore.Object, null, null, null, null, null, null, null, null);

            _service = new OrderService(
                _orderRepo.Object,
                _attachmentRepo.Object,
                _paymentService.Object,
                _paymentKeys.Object,
                _unitOfWork.Object,
                _emailService.Object,
                _userManager.Object,
                _productionService.Object,
                _kafkaProducer.Object
            );
        }
        [TearDown]
        public void TearDown()
        {
            _db.Dispose();
        }
        [Test]
        public async Task GetOrdersByUserIdAsync_ReturnsOrders()
        {
            var userId = "user-1";
            _orderRepo.Setup(r => r.GetOrdersByUserIdAsync(userId)).ReturnsAsync(new List<Order> {
                new Order { Id = Guid.NewGuid(), Name = "Order1", UserId = userId }
            });

            var result = await _service.GetOrdersByUserIdAsync(userId);
            Assert.That(result, Is.Not.Empty);
        }

        [Test]
        public async Task GetAllOrdersAsync_ReturnsAll()
        {
            _orderRepo.Setup(r => r.GetAllOrdersAsync()).ReturnsAsync(new List<Order> {
                new Order { Id = Guid.NewGuid(), Name = "AllOrder" }
            });

            var result = await _service.GetAllOrdersAsync();
            Assert.That(result, Is.Not.Empty);
        }

        [Test]
        public async Task UploadAttachmentsAsync_SavesAttachments()
        {
            var fileMock = new Mock<IFormFile>();
            fileMock.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), default))
                    .Returns(Task.CompletedTask);
            fileMock.Setup(f => f.FileName).Returns("test.pdf");
            fileMock.Setup(f => f.ContentType).Returns("application/pdf");
            fileMock.Setup(f => f.Length).Returns(100);

            await _service.UploadAttachmentsAsync(Guid.NewGuid(), new List<IFormFile> { fileMock.Object });
            _attachmentRepo.Verify(a => a.Add(It.IsAny<OrderAttachment>()), Times.AtLeastOnce);
        }

        [Test]
        public async Task GetOrderByIdAsync_ReturnsNull_IfUnauthorized()
        {
            _orderRepo.Setup(r => r.GetOrderByIdAsync(It.IsAny<Guid>())).ReturnsAsync(
                new Order { Id = Guid.NewGuid(), UserId = "different-user" });

            var principal = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim("userId", "user-123"),
                new Claim(ClaimTypes.Role, "User")
            }));

            var result = await _service.GetOrderByIdAsync(Guid.NewGuid(), principal);
            Assert.That(result, Is.Null);
        }

        [Test]
        public async Task UpdateOrderAmountAsync_UpdatesAmount()
        {
            var orderId = Guid.NewGuid();
            var order = new Order { Id = orderId, TotalAmount = 10 };
            _orderRepo.Setup(r => r.GetOrderByIdAsync(orderId)).ReturnsAsync(order);

            await _service.UpdateOrderAmountAsync(orderId, 99);
            Assert.That(order.TotalAmount, Is.EqualTo(99));
        }

        [Test]
        public async Task CheckoutOrderAsync_Fails_WhenKeyExists()
        {
            var orderId = Guid.NewGuid();
            _orderRepo.Setup(r => r.GetOrderByIdAsync(orderId)).ReturnsAsync(
                new Order { Id = orderId, Status = OrderStatus.Created });
            _paymentKeys.Setup(k => k.IsKeyUsedAsync(orderId, "key1")).ReturnsAsync(true);

            var (success, error) = await _service.CheckoutOrderAsync(orderId, "key1");
            Assert.That(success, Is.False);
            Assert.That(error, Does.Contain("Duplicate"));
        }

        [Test]
        public async Task PushToProductionAsync_ReturnsSuccess_IfValid()
        {
            var order = new Order { Id = Guid.NewGuid(), Status = OrderStatus.Paid };
            _orderRepo.Setup(r => r.GetOrderByIdAsync(order.Id)).ReturnsAsync(order);
            _orderRepo.Setup(r => r.GetActiveQueueByOrderIdAsync(order.Id)).ReturnsAsync((ProductionQueue?)null);

            var (success, _) = await _service.PushToProductionAsync(order.Id);
            Assert.That(success, Is.True);
        }

        [Test]
        public async Task TestQueueInsertAsync_InvokesQueueAndKafka()
        {
            await _service.TestQueueInsertAsync();
            _kafkaProducer.Verify(k => k.ProduceAsync("production-queue", It.IsAny<object>()), Times.Once);
        }

    }

}
