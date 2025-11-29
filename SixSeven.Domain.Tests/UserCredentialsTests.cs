using System;
using NUnit.Framework;
using SixSeven.Domain.Entities;
using SixSeven.Domain.Enums;

namespace SixSeven.Domain.Tests.Entities
{
    [TestFixture]
    public class UserCredentialsTests
    {
        [Test]
        public void Constructor_ValidArguments_SetsPropertiesCorrectly()
        {
            var email = "test@example.com";
            var passwordHash = "hashed-password";

            var user = new UserCredentials(email, passwordHash);

            Assert.Multiple(() =>
            {
                Assert.That(user.Id, Is.Not.Null.And.Not.Empty);
                Assert.That(user.Email, Is.EqualTo(email));
                Assert.That(user.PasswordHash, Is.EqualTo(passwordHash));
                Assert.That(user.Role, Is.EqualTo(UserRole.Volunteer));
                Assert.That(user.Notifications, Is.Not.Null);
                Assert.That(user.Notifications, Is.Empty);
                Assert.That(user.Profile, Is.Null);
            });
        }

        [Test]
        public void Constructor_ExplicitRole_SetsRoleCorrectly()
        {
            var user = new UserCredentials(
                "admin@example.com",
                "hash",
                UserRole.Admin);

            Assert.That(user.Role, Is.EqualTo(UserRole.Admin));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void Constructor_InvalidEmail_ThrowsArgumentException(string? invalidEmail)
        {
            var ex = Assert.Throws<ArgumentException>(() =>
                new UserCredentials(
                    invalidEmail!,
                    "hash"));

            Assert.That(ex!.ParamName, Is.EqualTo("email"));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void Constructor_InvalidPasswordHash_ThrowsArgumentException(string? invalidPassword)
        {
            var ex = Assert.Throws<ArgumentException>(() =>
                new UserCredentials(
                    "test@example.com",
                    invalidPassword!));

            Assert.That(ex!.ParamName, Is.EqualTo("passwordHash"));
        }

        [Test]
        public void ChangeRole_UpdatesRole()
        {
            var user = new UserCredentials(
                "test@example.com",
                "hash");

            user.ChangeRole(UserRole.Admin);

            Assert.That(user.Role, Is.EqualTo(UserRole.Admin));
        }

        [Test]
        public void SetPasswordHash_Valid_SetsPasswordHash()
        {
            var user = new UserCredentials(
                "test@example.com",
                "old-hash");

            user.SetPasswordHash("new-hash");

            Assert.That(user.PasswordHash, Is.EqualTo("new-hash"));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void SetPasswordHash_Invalid_ThrowsArgumentException(string? invalidPassword)
        {
            var user = new UserCredentials(
                "test@example.com",
                "hash");

            var ex = Assert.Throws<ArgumentException>(() =>
                user.SetPasswordHash(invalidPassword!));

            Assert.That(ex!.ParamName, Is.EqualTo("passwordHash"));
        }

        [Test]
        public void AttachProfile_ValidProfile_SetsProfile()
        {
            var user = new UserCredentials(
                "test@example.com",
                "hash");

            var profile = new UserProfile(
                user.Id,
                "John",
                "Doe",
                "123 Main St",
                "Houston",
                "TX",
                "77001",
                "Weekends only",
                new[] { VolunteerSkill.Cooking, VolunteerSkill.Cooking },
                null,
                new[] { "Saturday", "Sunday", "Saturday" });

            user.AttachProfile(profile);

            Assert.That(user.Profile, Is.SameAs(profile));
        }

        [Test]
        public void AttachProfile_NullProfile_ThrowsArgumentNullException()
        {
            var user = new UserCredentials(
                "test@example.com",
                "hash");

            var ex = Assert.Throws<ArgumentNullException>(() =>
                user.AttachProfile(null!));

            Assert.That(ex!.ParamName, Is.EqualTo("profile"));
        }

        [Test]
        public void Notifications_Collection_IsMutableAndInitialized()
        {
            var user = new UserCredentials(
                "test@example.com",
                "hash");

            user.Notifications.Add(new Notification(user.Id, "Test"));

            Assert.That(user.Notifications.Count, Is.EqualTo(1));
        }
    }
}
