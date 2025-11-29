using Microsoft.AspNetCore.Http;
using Moq;
using SixSeven.Application.Features.Authorization;
using SixSeven.Application.Interfaces.Repositories;
using SixSeven.Application.Interfaces.Services;
using SixSeven.Domain.DTO;
using SixSeven.Domain.Entities;
using SixSeven.Domain.Enums;

namespace SixSeven.Application.Tests.Features.Authorization
{
    [TestFixture]
    public class LoginCommandHandlerTests
    {
        private Mock<IGenericRepository<UserCredentials>> _credentialsRepo = null!;
        private Mock<IEncryptionService> _encryption = null!;
        private Mock<IJwtService> _jwt = null!;
        private Mock<IHttpContextAccessor> _httpContextAccessor = null!;
        private LoginCommandHandler _handler = null!;

        [SetUp]
        public void SetUp()
        {
            _credentialsRepo = new Mock<IGenericRepository<UserCredentials>>(MockBehavior.Strict);
            _encryption = new Mock<IEncryptionService>(MockBehavior.Strict);
            _jwt = new Mock<IJwtService>(MockBehavior.Strict);
            _httpContextAccessor = new Mock<IHttpContextAccessor>(MockBehavior.Strict);

            _handler = new LoginCommandHandler(
                _credentialsRepo.Object,
                _encryption.Object,
                _jwt.Object,
                _httpContextAccessor.Object);
        }

        [Test]
        public async Task Handle_ValidCredentials_SetsCookieAndReturnsAuthResponse()
        {
            var dto = new LoginRequest("user@test.com", "password");
            var creds = new UserCredentials(dto.Email, "hashed-password", UserRole.Admin);

            var httpContext = new DefaultHttpContext();
            _httpContextAccessor.SetupGet(a => a.HttpContext).Returns(httpContext);

            _credentialsRepo
                .Setup(r => r.FindAsync(
                    It.IsAny<System.Linq.Expressions.Expression<Func<UserCredentials, bool>>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(creds);

            _encryption
                .Setup(e => e.VerifyPassword(dto.Password, creds.PasswordHash))
                .Returns(true);

            _jwt
                .Setup(j => j.GenerateToken(creds.Id, creds.Email, creds.Role.ToString()))
                .Returns("jwt-token-123");

            var result = await _handler.Handle(new LoginCommand(dto), CancellationToken.None);

            Assert.Multiple(() =>
            {
                Assert.That(result.Id, Is.EqualTo(creds.Id));
                Assert.That(result.Email, Is.EqualTo(creds.Email));
                Assert.That(result.Role, Is.EqualTo(creds.Role.ToString()));
            });

            var setCookieHeader = httpContext.Response.Headers["Set-Cookie"].ToString();
            Assert.That(setCookieHeader, Does.Contain("auth_token="));
            Assert.That(setCookieHeader, Does.Contain("jwt-token-123"));

            _credentialsRepo.VerifyAll();
            _encryption.VerifyAll();
            _jwt.VerifyAll();
            _httpContextAccessor.VerifyAll();
        }

        [Test]
        public void Handle_CredentialsNotFound_ThrowsInvalidOperationException()
        {
            var dto = new LoginRequest("user@test.com", "password");

            var httpContext = new DefaultHttpContext();
            _httpContextAccessor.SetupGet(a => a.HttpContext).Returns(httpContext);

            _credentialsRepo
                .Setup(r => r.FindAsync(
                    It.IsAny<System.Linq.Expressions.Expression<Func<UserCredentials, bool>>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((UserCredentials?)null);

            var ex = Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await _handler.Handle(new LoginCommand(dto), CancellationToken.None));

            Assert.That(ex!.Message, Is.EqualTo("Invalid email or password."));

            _credentialsRepo.VerifyAll();
            _encryption.Verify(e => e.VerifyPassword(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            _jwt.Verify(j => j.GenerateToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Test]
        public void Handle_InvalidPassword_ThrowsInvalidOperationException()
        {
            var dto = new LoginRequest("user@test.com", "bad-password");
            var creds = new UserCredentials(dto.Email, "hashed-password", UserRole.Volunteer);

            var httpContext = new DefaultHttpContext();
            _httpContextAccessor.SetupGet(a => a.HttpContext).Returns(httpContext);

            _credentialsRepo
                .Setup(r => r.FindAsync(
                    It.IsAny<System.Linq.Expressions.Expression<Func<UserCredentials, bool>>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(creds);

            _encryption
                .Setup(e => e.VerifyPassword(dto.Password, creds.PasswordHash))
                .Returns(false);

            var ex = Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await _handler.Handle(new LoginCommand(dto), CancellationToken.None));

            Assert.That(ex!.Message, Is.EqualTo("Invalid email or password."));

            _credentialsRepo.VerifyAll();
            _encryption.VerifyAll();
            _jwt.Verify(j => j.GenerateToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Test]
        public void Handle_NoHttpContext_ThrowsInvalidOperationException()
        {
            var dto = new LoginRequest("user@test.com", "password");
            var creds = new UserCredentials(dto.Email, "hashed-password", UserRole.Volunteer);

            _httpContextAccessor.SetupGet(a => a.HttpContext).Returns((HttpContext?)null);

            _credentialsRepo
                .Setup(r => r.FindAsync(
                    It.IsAny<System.Linq.Expressions.Expression<Func<UserCredentials, bool>>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(creds);

            _encryption
                .Setup(e => e.VerifyPassword(dto.Password, creds.PasswordHash))
                .Returns(true);

            _jwt
                .Setup(j => j.GenerateToken(creds.Id, creds.Email, creds.Role.ToString()))
                .Returns("jwt-token-123");

            var ex = Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await _handler.Handle(new LoginCommand(dto), CancellationToken.None));

            Assert.That(ex!.Message, Is.EqualTo("No HttpContext available."));

            _credentialsRepo.VerifyAll();
            _encryption.VerifyAll();
            _jwt.VerifyAll();
        }
    }
}
