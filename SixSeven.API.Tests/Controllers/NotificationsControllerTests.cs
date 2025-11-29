using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;
using SixSeven.Application.Dtos;
using SixSeven.Application.Features.Notifications;
using SixSeven.Controllers;

namespace SixSeven.API.Tests.Controllers
{
    [TestFixture]
    public class NotificationsControllerTests
    {
        private Mock<IMediator> _mediator = null!;
        private NotificationsController _controller = null!;

        [SetUp]
        public void SetUp()
        {
            _mediator = new Mock<IMediator>();
            _controller = new NotificationsController(_mediator.Object);
        }

        [Test]
        public async Task GetNotifications_NoUserId_ReturnsUnauthorized()
        {
            var httpContext = new DefaultHttpContext();
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            var result = await _controller.GetNotifications();

            Assert.That(result, Is.InstanceOf<UnauthorizedResult>());
            _mediator.Verify(
                m => m.Send(It.IsAny<GetNotificationsForUserQuery>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Test]
        public async Task GetNotifications_WithUserId_ReturnsOkWithNotifications()
        {
            var userId = "user-1";
            var identity = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId)
            }, "TestAuth");
            var principal = new ClaimsPrincipal(identity);

            var httpContext = new DefaultHttpContext
            {
                User = principal
            };

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            var notifications = new List<NotificationDto>
            {
                new()
                {
                    Id = "n1",
                    UserId = userId,
                    Message = "Test 1",
                    Read = false,
                    CreatedAt = DateTime.UtcNow
                },
                new()
                {
                    Id = "n2",
                    UserId = userId,
                    Message = "Test 2",
                    Read = true,
                    CreatedAt = DateTime.UtcNow
                }
            };

            _mediator
                .Setup(m => m.Send(
                    It.IsAny<GetNotificationsForUserQuery>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(notifications);

            var result = await _controller.GetNotifications();

            Assert.That(result, Is.InstanceOf<OkObjectResult>());
            var ok = (OkObjectResult)result;
            var value = ok.Value as IReadOnlyList<NotificationDto>;
            Assert.That(value, Is.Not.Null);
            Assert.That(value!.Count, Is.EqualTo(2));

            _mediator.Verify(
                m => m.Send(
                    It.Is<GetNotificationsForUserQuery>(q => q.UserId == userId),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Test]
        public async Task MarkAllRead_NoUserId_ReturnsUnauthorized()
        {
            var httpContext = new DefaultHttpContext();
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            var result = await _controller.MarkAllRead(CancellationToken.None);

            Assert.That(result, Is.InstanceOf<UnauthorizedResult>());
            _mediator.Verify(
                m => m.Send(It.IsAny<MarkAllNotificationsReadCommand>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Test]
        public async Task MarkAllRead_WithUserId_CallsMediatorAndReturnsOkWithUpdatedCount()
        {
            var userId = "user-1";
            var identity = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId)
            }, "TestAuth");
            var principal = new ClaimsPrincipal(identity);

            var httpContext = new DefaultHttpContext
            {
                User = principal
            };

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            const int updatedCount = 3;

            _mediator
                .Setup(m => m.Send(
                    It.IsAny<MarkAllNotificationsReadCommand>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(updatedCount);

            var result = await _controller.MarkAllRead(CancellationToken.None);

            Assert.That(result, Is.InstanceOf<OkObjectResult>());
            var ok = (OkObjectResult)result;
            Assert.That(ok.Value, Is.Not.Null);

            var updatedProperty = ok.Value!.GetType().GetProperty("updated");
            Assert.That(updatedProperty, Is.Not.Null);
            var value = updatedProperty!.GetValue(ok.Value);
            Assert.That(value, Is.EqualTo(updatedCount));

            _mediator.Verify(
                m => m.Send(
                    It.Is<MarkAllNotificationsReadCommand>(c => c.UserId == userId),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }
}
