using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using SixSeven.Data;
using SixSeven.Data.ReadStores;
using SixSeven.Domain.Entities;
using SixSeven.Domain.Enums;

namespace SixSeven.Data.Tests.ReadStores
{
    [TestFixture]
    public class UserReadStoreTests
    {
        private AppDbContext _context = null!;
        private UserReadStore _store = null!;

        [SetUp]
        public void SetUp()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase($"user-readstore-{Guid.NewGuid():N}")
                .Options;

            _context = new AppDbContext(options);
            _store = new UserReadStore(_context);
        }

        private UserCredentials CreateUser(string email, UserRole role = UserRole.Volunteer)
        {
            var user = new UserCredentials(email, "hash", role);
            _context.UserCredentials.Add(user);
            return user;
        }

        [Test]
        public async Task GetByIdAsync_ValidId_ReturnsUser()
        {
            var user = CreateUser("user@test.com");
            await _context.SaveChangesAsync();

            var result = await _store.GetByIdAsync(user.Id, CancellationToken.None);

            Assert.That(result, Is.Not.Null);
            Assert.That(result!.Id, Is.EqualTo(user.Id));
            Assert.That(result.Email, Is.EqualTo(user.Email));
        }

        [Test]
        public async Task GetByIdAsync_UnknownId_ReturnsNull()
        {
            CreateUser("user@test.com");
            await _context.SaveChangesAsync();

            var result = await _store.GetByIdAsync("does-not-exist", CancellationToken.None);

            Assert.That(result, Is.Null);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public async Task GetByIdAsync_NullOrWhitespaceId_ReturnsNull(string? id)
        {
            var result = await _store.GetByIdAsync(id!, CancellationToken.None);

            Assert.That(result, Is.Null);
        }

        [Test]
        public async Task GetByEmailAsync_ValidEmail_ReturnsUser()
        {
            var user = CreateUser("user@test.com");
            await _context.SaveChangesAsync();

            var result = await _store.GetByEmailAsync("user@test.com", CancellationToken.None);

            Assert.That(result, Is.Not.Null);
            Assert.That(result!.Id, Is.EqualTo(user.Id));
            Assert.That(result.Email, Is.EqualTo(user.Email));
        }

        [Test]
        public async Task GetByEmailAsync_UnknownEmail_ReturnsNull()
        {
            CreateUser("user@test.com");
            await _context.SaveChangesAsync();

            var result = await _store.GetByEmailAsync("other@test.com", CancellationToken.None);

            Assert.That(result, Is.Null);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public async Task GetByEmailAsync_NullOrWhitespaceEmail_ReturnsNull(string? email)
        {
            var result = await _store.GetByEmailAsync(email!, CancellationToken.None);

            Assert.That(result, Is.Null);
        }

        [Test]
        public async Task GetRoleByIdAsync_ExistingUser_ReturnsRole()
        {
            var user = CreateUser("admin@test.com", UserRole.Admin);
            await _context.SaveChangesAsync();

            var role = await _store.GetRoleByIdAsync(user.Id, CancellationToken.None);

            Assert.That(role, Is.EqualTo(UserRole.Admin));
        }

        [Test]
        public async Task GetRoleByIdAsync_UnknownUser_ReturnsNull()
        {
            CreateUser("user@test.com", UserRole.Volunteer);
            await _context.SaveChangesAsync();

            var role = await _store.GetRoleByIdAsync("does-not-exist", CancellationToken.None);

            Assert.That(role, Is.Null);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public async Task GetRoleByIdAsync_NullOrWhitespaceId_ReturnsNull(string? id)
        {
            var role = await _store.GetRoleByIdAsync(id!, CancellationToken.None);

            Assert.That(role, Is.Null);
        }

        [Test]
        public async Task GetRoleByEmailAsync_ExistingUser_ReturnsRole()
        {
            var user = CreateUser("admin@test.com", UserRole.Admin);
            await _context.SaveChangesAsync();

            var role = await _store.GetRoleByEmailAsync("admin@test.com", CancellationToken.None);

            Assert.That(role, Is.EqualTo(UserRole.Admin));
        }

        [Test]
        public async Task GetRoleByEmailAsync_UnknownEmail_ReturnsNull()
        {
            CreateUser("user@test.com", UserRole.Volunteer);
            await _context.SaveChangesAsync();

            var role = await _store.GetRoleByEmailAsync("other@test.com", CancellationToken.None);

            Assert.That(role, Is.Null);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public async Task GetRoleByEmailAsync_NullOrWhitespaceEmail_ReturnsNull(string? email)
        {
            var role = await _store.GetRoleByEmailAsync(email!, CancellationToken.None);

            Assert.That(role, Is.Null);
        }
    }
}
