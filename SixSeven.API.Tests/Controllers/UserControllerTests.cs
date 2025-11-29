using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;
using SixSeven.Application.Dtos;
using SixSeven.Application.Features.Users;
using SixSeven.Controllers;
using SixSeven.Domain.DTO;
using SixSeven.Domain.Enums;

namespace SixSeven.API.Tests.Controllers
{
    [TestFixture]
    public class UserControllerTests
    {
        private Mock<ISender> _mediator = null!;
        private UserController _controller = null!;

        [SetUp]
        public void SetUp()
        {
            _mediator = new Mock<ISender>();
            _controller = new UserController(_mediator.Object);
        }

        [Test]
        public async Task GetMe_NoUserId_ReturnsUnauthorized()
        {
            var httpContext = new DefaultHttpContext();
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            var result = await _controller.GetMe();

            Assert.That(result.Result, Is.InstanceOf<UnauthorizedResult>());

            _mediator.Verify(
                m => m.Send(It.IsAny<GetCurrentUserQuery>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Test]
        public async Task GetMe_UserNotFound_ReturnsNotFound()
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

            _mediator
                .Setup(m => m.Send(
                    It.IsAny<GetCurrentUserQuery>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((UserDto?)null);

            var result = await _controller.GetMe();

            Assert.That(result.Result, Is.InstanceOf<NotFoundResult>());

            _mediator.Verify(
                m => m.Send(
                    It.Is<GetCurrentUserQuery>(q => q.UserId == userId),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Test]
        public async Task GetMe_UserFound_ReturnsOkWithUserDto()
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

            var dto = new UserDto
            {
                Id = userId,
                Email = "user@test.com",
                Role = UserRole.Volunteer,
                FirstName = "Alice",
                LastName = "Smith",
                AddressOne = "123 Main",
                City = "City",
                State = "TX",
                ZipCode = "77001",
                Skills = new List<VolunteerSkill> { VolunteerSkill.Cooking },
                Preferences = "Pref",
                Availability = new List<string> { "Mon" }
            };

            _mediator
                .Setup(m => m.Send(
                    It.IsAny<GetCurrentUserQuery>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(dto);

            var result = await _controller.GetMe();

            Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
            var ok = (OkObjectResult)result.Result!;
            Assert.That(ok.Value, Is.InstanceOf<UserDto>());
            var returned = (UserDto)ok.Value!;
            Assert.That(returned.Id, Is.EqualTo(userId));
            Assert.That(returned.Email, Is.EqualTo("user@test.com"));

            _mediator.Verify(
                m => m.Send(
                    It.Is<GetCurrentUserQuery>(q => q.UserId == userId),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Test]
        public async Task UpdateUser_NoUserId_ReturnsUnauthorized()
        {
            var httpContext = new DefaultHttpContext();
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            var request = new UpdateUserRequest
            {
                FirstName = "Alice"
            };

            var result = await _controller.UpdateUser(request);

            Assert.That(result.Result, Is.InstanceOf<UnauthorizedResult>());

            _mediator.Verify(
                m => m.Send(It.IsAny<UpdateUserCommand>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Test]
        public async Task UpdateUser_ValidRequest_SendsCommandAndReturnsOk()
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

            var request = new UpdateUserRequest
            {
                FirstName = "Alice",
                LastName = "Smith",
                AddressOne = "123 Main",
                City = "City",
                State = "TX",
                ZipCode = "77001",
                Preferences = "Pref",
                Skills = new[] { "Cooking" },
                Availability = new List<string> { "Mon" }
            };

            var updatedDto = new UserDto
            {
                Id = userId,
                Email = "user@test.com",
                Role = UserRole.Volunteer,
                FirstName = "Alice",
                LastName = "Smith"
            };

            _mediator
                .Setup(m => m.Send(
                    It.IsAny<UpdateUserCommand>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(updatedDto);

            var result = await _controller.UpdateUser(request);

            Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
            var ok = (OkObjectResult)result.Result!;
            Assert.That(ok.Value, Is.InstanceOf<UserDto>());
            var returned = (UserDto)ok.Value!;
            Assert.That(returned.Id, Is.EqualTo(userId));
            Assert.That(returned.FirstName, Is.EqualTo("Alice"));

            _mediator.Verify(
                m => m.Send(
                    It.Is<UpdateUserCommand>(c =>
                        c.UserId == userId &&
                        c.Request == request),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }
}
