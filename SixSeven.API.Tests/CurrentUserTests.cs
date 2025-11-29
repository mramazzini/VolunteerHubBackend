using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Moq;
using SixSeven.Application.Authorization;
using SixSeven.Auth;

namespace SixSeven.API.Tests
{
    [TestFixture]
    public class CurrentUserTests
    {
        [Test]
        public void UserId_NameIdentifierClaimPresent_ReturnsGuid()
        {
            var id = Guid.NewGuid();
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, id.ToString())
            };
            var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));

            var httpContext = new DefaultHttpContext
            {
                User = principal
            };

            var accessor = new Mock<IHttpContextAccessor>();
            accessor.Setup(a => a.HttpContext).Returns(httpContext);

            ICurrentUser currentUser = new CurrentUser(accessor.Object);

            Assert.That(currentUser.UserId, Is.EqualTo(id));
        }

        [Test]
        public void UserId_SubClaimUsedWhenNameIdentifierMissing_ReturnsGuid()
        {
            var id = Guid.NewGuid();
            var claims = new[]
            {
                new Claim("sub", id.ToString())
            };
            var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));

            var httpContext = new DefaultHttpContext
            {
                User = principal
            };

            var accessor = new Mock<IHttpContextAccessor>();
            accessor.Setup(a => a.HttpContext).Returns(httpContext);

            ICurrentUser currentUser = new CurrentUser(accessor.Object);

            Assert.That(currentUser.UserId, Is.EqualTo(id));
        }

        [Test]
        public void UserId_PrefersNameIdentifierOverSub()
        {
            var idName = Guid.NewGuid();
            var idSub = Guid.NewGuid();

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, idName.ToString()),
                new Claim("sub", idSub.ToString())
            };
            var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));

            var httpContext = new DefaultHttpContext
            {
                User = principal
            };

            var accessor = new Mock<IHttpContextAccessor>();
            accessor.Setup(a => a.HttpContext).Returns(httpContext);

            ICurrentUser currentUser = new CurrentUser(accessor.Object);

            Assert.That(currentUser.UserId, Is.EqualTo(idName));
        }

        [Test]
        public void UserId_InvalidGuidClaim_ReturnsNull()
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "not-a-guid")
            };
            var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));

            var httpContext = new DefaultHttpContext
            {
                User = principal
            };

            var accessor = new Mock<IHttpContextAccessor>();
            accessor.Setup(a => a.HttpContext).Returns(httpContext);

            ICurrentUser currentUser = new CurrentUser(accessor.Object);

            Assert.That(currentUser.UserId, Is.Null);
        }

        [Test]
        public void UserId_NoHttpContext_ReturnsNull()
        {
            var accessor = new Mock<IHttpContextAccessor>();
            accessor.Setup(a => a.HttpContext).Returns((HttpContext?)null);

            ICurrentUser currentUser = new CurrentUser(accessor.Object);

            Assert.That(currentUser.UserId, Is.Null);
        }

        [Test]
        public void UserId_NoRelevantClaims_ReturnsNull()
        {
            var principal = new ClaimsPrincipal(new ClaimsIdentity());

            var httpContext = new DefaultHttpContext
            {
                User = principal
            };

            var accessor = new Mock<IHttpContextAccessor>();
            accessor.Setup(a => a.HttpContext).Returns(httpContext);

            ICurrentUser currentUser = new CurrentUser(accessor.Object);

            Assert.That(currentUser.UserId, Is.Null);
        }
    }
}
