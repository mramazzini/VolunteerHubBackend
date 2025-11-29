using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;
using SixSeven.Application.Features.Authorization;
using SixSeven.Controllers;
using SixSeven.Domain.DTO;

namespace SixSeven.API.Tests.Controllers
{
    [TestFixture]
    public class AuthControllerTests
    {
        private Mock<IMediator> _mediator = null!;
        private AuthController _controller = null!;

        [SetUp]
        public void SetUp()
        {
            _mediator = new Mock<IMediator>();
            _controller = new AuthController(_mediator.Object);
        }

        [Test]
        public async Task Login_ValidRequest_ReturnsOkWithAuthResponse()
        {
            var request = new LoginRequest(
                Email: "test@example.com",
                Password: "Password123!");

            var expected = new AuthResponse(
               Id: "user-1",
                Email: request.Email,
                Role: "Volunteer");

            _mediator
                .Setup(m => m.Send(It.IsAny<LoginCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expected);

            var result = await _controller.Login(request);

            Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
            var ok = (OkObjectResult)result.Result!;
            Assert.That(ok.Value, Is.EqualTo(expected));

            _mediator.Verify(
                m => m.Send(It.Is<LoginCommand>(c => c.Request == request),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Test]
        public async Task Signup_ValidRequest_ReturnsOkWithAuthResponse()
        {
            var request = new SignupRequest(
                Email: "new@example.com",
                Password: "Password123!");

            var expected = new AuthResponse(
                Id: "user-2",
                Email: request.Email,
                Role: "Volunteer");

            _mediator
                .Setup(m => m.Send(It.IsAny<SignupCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expected);

            var result = await _controller.Signup(request);

            Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
            var ok = (OkObjectResult)result.Result!;
            Assert.That(ok.Value, Is.EqualTo(expected));

            _mediator.Verify(
                m => m.Send(It.Is<SignupCommand>(c => c.Request == request),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Test]
        public async Task Logout_NoUserId_ReturnsUnauthorized()
        {
            var httpContext = new DefaultHttpContext();
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            var result = await _controller.Logout();

            Assert.That(result, Is.InstanceOf<UnauthorizedResult>());

            _mediator.Verify(
                m => m.Send(It.IsAny<LogoutCommand>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Test]
        public async Task Logout_WithUserId_SendsCommandAndReturnsOk()
        {
            var userId = "user-123";
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

            _mediator
                .Setup(m => m.Send(It.IsAny<LogoutCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Unit.Value);

            var result = await _controller.Logout();

            Assert.That(result, Is.InstanceOf<OkResult>());

            _mediator.Verify(
                m => m.Send(
                    It.Is<LogoutCommand>(c => c.UserId == userId),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }
}
