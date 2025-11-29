using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Moq;
using SixSeven.Application.Authorization;
using SixSeven.Application.Interfaces.ReadStore;
using SixSeven.Auth.Handlers;
using SixSeven.Domain.Enums;

namespace SixSeven.API.Tests
{
    [TestFixture]
    public class AdminRequirementHandlerTests
    {
        private Mock<IUserReadStore> _userReadStore = null!;
        private AdminRequirementHandler _handler = null!;

        [SetUp]
        public void SetUp()
        {
            _userReadStore = new Mock<IUserReadStore>(MockBehavior.Strict);
            _handler = new AdminRequirementHandler(_userReadStore.Object);
        }

        private static AuthorizationHandlerContext CreateContext(
            ClaimsPrincipal user,
            AdminRequirement requirement)
        {
            return new AuthorizationHandlerContext(
                new[] { requirement },
                user,
                resource: null);
        }

        [Test]
        public async Task HandleRequirementAsync_NoSubClaim_DoesNotSucceedAndDoesNotCallStore()
        {
            var requirement = new AdminRequirement();
            var user = new ClaimsPrincipal(new ClaimsIdentity());
            var context = CreateContext(user, requirement);

            await _handler.HandleAsync(context);

            Assert.That(context.HasSucceeded, Is.False);
            _userReadStore.Verify(
                s => s.GetRoleByIdAsync(It.IsAny<string>(), default),
                Times.Never);
        }

        [Test]
        public async Task HandleRequirementAsync_NonAdminRole_DoesNotSucceed()
        {
            var requirement = new AdminRequirement();
            var userId = "user-123";
            var user = new ClaimsPrincipal(
                new ClaimsIdentity(new[] { new Claim("sub", userId) }));

            _userReadStore
                .Setup(s => s.GetRoleByIdAsync(userId, default))
                .ReturnsAsync(UserRole.Volunteer);

            var context = CreateContext(user, requirement);

            await _handler.HandleAsync(context);

            Assert.That(context.HasSucceeded, Is.False);
            _userReadStore.Verify(
                s => s.GetRoleByIdAsync(userId, default),
                Times.Once);
        }

        [Test]
        public async Task HandleRequirementAsync_AdminRole_Succeeds()
        {
            var requirement = new AdminRequirement();
            var userId = "admin-123";
            var user = new ClaimsPrincipal(
                new ClaimsIdentity(new[] { new Claim("sub", userId) }));

            _userReadStore
                .Setup(s => s.GetRoleByIdAsync(userId, default))
                .ReturnsAsync(UserRole.Admin);

            var context = CreateContext(user, requirement);

            await _handler.HandleAsync(context);

            Assert.That(context.HasSucceeded, Is.True);
            Assert.That(context.PendingRequirements, Is.Empty);
            _userReadStore.Verify(
                s => s.GetRoleByIdAsync(userId, default),
                Times.Once);
        }

        [Test]
        public async Task HandleRequirementAsync_UserNotFound_DoesNotSucceed()
        {
            var requirement = new AdminRequirement();
            var userId = "missing-user";
            var user = new ClaimsPrincipal(
                new ClaimsIdentity(new[] { new Claim("sub", userId) }));

            _userReadStore
                .Setup(s => s.GetRoleByIdAsync(userId, default))
                .ReturnsAsync((UserRole?)null);

            var context = CreateContext(user, requirement);

            await _handler.HandleAsync(context);

            Assert.That(context.HasSucceeded, Is.False);
            _userReadStore.Verify(
                s => s.GetRoleByIdAsync(userId, default),
                Times.Once);
        }
    }
}
