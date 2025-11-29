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
    public class SignupCommandHandlerTests
    {
        private Mock<IGenericRepository<UserCredentials>> _credentialsRepo = null!;
        private Mock<IGenericRepository<Notification>> _notificationRepo = null!;
        private Mock<IEncryptionService> _encryption = null!;
        private Mock<IJwtService> _jwt = null!;
        private Mock<IHttpContextAccessor> _httpContextAccessor = null!;
        private SignupCommandHandler _handler = null!;

        [SetUp]
        public void SetUp()
        {
            _credentialsRepo = new Mock<IGenericRepository<UserCredentials>>(MockBehavior.Strict);
            _notificationRepo = new Mock<IGenericRepository<Notification>>(MockBehavior.Strict);
            _encryption = new Mock<IEncryptionService>(MockBehavior.Strict);
            _jwt = new Mock<IJwtService>(MockBehavior.Strict);
            _httpContextAccessor = new Mock<IHttpContextAccessor>(MockBehavior.Strict);

            _handler = new SignupCommandHandler(
                _credentialsRepo.Object,
                _notificationRepo.Object,
                _encryption.Object,
                _jwt.Object,
                _httpContextAccessor.Object);
        }

        [Test]
        public async Task Handle_NewUser_CreatesUserNotificationSetsCookieAndReturnsAuthResponse()
        {
            var dto = new SignupRequest("user@test.com", "password");
            var httpContext = new DefaultHttpContext();
            _httpContextAccessor.SetupGet(a => a.HttpContext).Returns(httpContext);

            _credentialsRepo
                .Setup(r => r.FindAsync(
                    It.IsAny<System.Linq.Expressions.Expression<Func<UserCredentials, bool>>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((UserCredentials?)null);

            _encryption
                .Setup(e => e.HashPassword(dto.Password))
                .Returns("hashed-password");

            UserCredentials? createdCredentials = null;
            _credentialsRepo
                .Setup(r => r.QueueInsert(It.IsAny<UserCredentials>(), It.IsAny<CancellationToken>()))
                .Callback<UserCredentials, CancellationToken>((c, _) => createdCredentials = c);

            _credentialsRepo
                .Setup(r => r.SaveAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            Notification? createdNotification = null;
            _notificationRepo
                .Setup(r => r.QueueInsert(It.IsAny<Notification>(), It.IsAny<CancellationToken>()))
                .Callback<Notification, CancellationToken>((n, _) => createdNotification = n);

            _notificationRepo
                .Setup(r => r.SaveAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            _jwt
                .Setup(j => j.GenerateToken(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .Returns("jwt-token-123");

            var result = await _handler.Handle(new SignupCommand(dto), CancellationToken.None);

            Assert.Multiple(() =>
            {
                Assert.That(result.Email, Is.EqualTo(dto.Email));
                Assert.That(result.Role, Is.EqualTo(UserRole.Volunteer.ToString()));
                Assert.That(result.Id, Is.EqualTo(createdCredentials!.Id));
            });

            Assert.That(createdCredentials, Is.Not.Null);
            Assert.That(createdCredentials!.Email, Is.EqualTo(dto.Email));
            Assert.That(createdCredentials.PasswordHash, Is.EqualTo("hashed-password"));
            Assert.That(createdCredentials.Role, Is.EqualTo(UserRole.Volunteer));

            Assert.That(createdNotification, Is.Not.Null);
            Assert.That(createdNotification!.UserId, Is.EqualTo(createdCredentials.Id));
            Assert.That(createdNotification.Message, Does.Contain("Welcome to Volunteer Hub"));

            var setCookieHeader = httpContext.Response.Headers["Set-Cookie"].ToString();
            Assert.That(setCookieHeader, Does.Contain("auth_token="));
            Assert.That(setCookieHeader, Does.Contain("jwt-token-123"));

            _credentialsRepo.VerifyAll();
            _notificationRepo.VerifyAll();
            _encryption.VerifyAll();
            _jwt.VerifyAll();
            _httpContextAccessor.VerifyAll();
        }

        [Test]
        public void Handle_UserAlreadyExists_ThrowsInvalidOperationException()
        {
            var dto = new SignupRequest("user@test.com", "password");
            var existing = new UserCredentials(dto.Email, "existing-hash", UserRole.Volunteer);

            var httpContext = new DefaultHttpContext();
            _httpContextAccessor.SetupGet(a => a.HttpContext).Returns(httpContext);

            _credentialsRepo
                .Setup(r => r.FindAsync(
                    It.IsAny<System.Linq.Expressions.Expression<Func<UserCredentials, bool>>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(existing);

            var ex = Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await _handler.Handle(new SignupCommand(dto), CancellationToken.None));

            Assert.That(ex!.Message, Is.EqualTo("A user with this email already exists."));

            _credentialsRepo.VerifyAll();
            _encryption.Verify(e => e.HashPassword(It.IsAny<string>()), Times.Never);
            _notificationRepo.Verify(r => r.QueueInsert(It.IsAny<Notification>(), It.IsAny<CancellationToken>()), Times.Never);
            _notificationRepo.Verify(r => r.SaveAsync(It.IsAny<CancellationToken>()), Times.Never);
            _jwt.Verify(j => j.GenerateToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Test]
        public void Handle_NoHttpContext_ThrowsInvalidOperationException()
        {
            var dto = new SignupRequest("user@test.com", "password");

            _httpContextAccessor.SetupGet(a => a.HttpContext).Returns((HttpContext?)null);

            _credentialsRepo
                .Setup(r => r.FindAsync(
                    It.IsAny<System.Linq.Expressions.Expression<Func<UserCredentials, bool>>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((UserCredentials?)null);

            _encryption
                .Setup(e => e.HashPassword(dto.Password))
                .Returns("hashed-password");

            _credentialsRepo
                .Setup(r => r.QueueInsert(It.IsAny<UserCredentials>(), It.IsAny<CancellationToken>()));

            _credentialsRepo
                .Setup(r => r.SaveAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            _notificationRepo
                .Setup(r => r.QueueInsert(It.IsAny<Notification>(), It.IsAny<CancellationToken>()));

            _notificationRepo
                .Setup(r => r.SaveAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            _jwt
                .Setup(j => j.GenerateToken(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .Returns("jwt-token-123");

            var ex = Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await _handler.Handle(new SignupCommand(dto), CancellationToken.None));

            Assert.That(ex!.Message, Is.EqualTo("No HttpContext available."));

            _credentialsRepo.VerifyAll();
            _notificationRepo.VerifyAll();
            _encryption.VerifyAll();
            _jwt.VerifyAll();
        }
    }
}
