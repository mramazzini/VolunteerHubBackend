using MediatR;
using Microsoft.AspNetCore.Http;
using Moq;
using SixSeven.Application.Features.Authorization;

namespace SixSeven.Application.Tests.Features.Authorization
{
    [TestFixture]
    public class LogoutCommandHandlerTests
    {
        private Mock<IHttpContextAccessor> _httpContextAccessor = null!;
        private LogoutCommandHandler _handler = null!;

        [SetUp]
        public void SetUp()
        {
            _httpContextAccessor = new Mock<IHttpContextAccessor>(MockBehavior.Strict);
            _handler = new LogoutCommandHandler(_httpContextAccessor.Object);
        }

        [Test]
        public async Task Handle_WithHttpContext_ClearsAuthCookieAndReturnsUnit()
        {
            var httpContext = new DefaultHttpContext();
            _httpContextAccessor
                .SetupGet(a => a.HttpContext)
                .Returns(httpContext);

            var result = await _handler.Handle(
                new LogoutCommand("user-123"),
                CancellationToken.None);

            Assert.That(result, Is.EqualTo(Unit.Value));

            var setCookieHeader = httpContext.Response.Headers["Set-Cookie"].ToString();
            Assert.That(setCookieHeader, Does.Contain("auth_token="));
            Assert.That(setCookieHeader, Does.Contain("expires="));
        }

        [Test]
        public void Handle_NoHttpContext_ThrowsInvalidOperationException()
        {
            _httpContextAccessor
                .SetupGet(a => a.HttpContext)
                .Returns((HttpContext?)null);

            var ex = Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await _handler.Handle(
                    new LogoutCommand("user-123"),
                    CancellationToken.None));

            Assert.That(ex!.Message, Is.EqualTo("No HttpContext available."));
        }
    }
}