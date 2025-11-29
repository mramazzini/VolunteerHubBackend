using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using SixSeven.Application.Dtos;
using SixSeven.Application.Features.Users;
using SixSeven.Application.Interfaces.Repositories;
using SixSeven.Domain.Entities;
using SixSeven.Domain.Enums;

namespace SixSeven.Application.Tests.Features.Users
{
    [TestFixture]
    public class GetCurrentUserQueryHandlerTests
    {
        private Mock<IGenericRepository<UserCredentials>> _credentialsRepo = null!;
        private Mock<IGenericRepository<UserProfile>> _profileRepo = null!;
        private GetCurrentUserQueryHandler _handler = null!;

        [SetUp]
        public void SetUp()
        {
            _credentialsRepo = new Mock<IGenericRepository<UserCredentials>>(MockBehavior.Strict);
            _profileRepo = new Mock<IGenericRepository<UserProfile>>(MockBehavior.Strict);
            _handler = new GetCurrentUserQueryHandler(_credentialsRepo.Object, _profileRepo.Object);
        }

        private static UserCredentials CreateCredentials(string id, string email, UserRole role = UserRole.Volunteer)
        {
            var creds = new UserCredentials(email, "hash", role);
            typeof(UserCredentials).GetProperty(nameof(UserCredentials.Id))!
                .SetValue(creds, id);
            return creds;
        }

        private static UserProfile CreateProfile(
            string userCredentialsId,
            string firstName = "Alice",
            string lastName = "Smith")
        {
            var profile = new UserProfile(
                userCredentialsId: userCredentialsId,
                firstName: firstName,
                lastName: lastName,
                addressOne: "123 Main",
                city: "City",
                state: "TX",
                zipCode: "77001",
                preferences: "Pref",
                skills: new List<VolunteerSkill> { VolunteerSkill.Cooking },
                otherSkills: null,
                availability: new List<string> { "Weekends" });

            typeof(UserProfile).GetProperty(nameof(UserProfile.Id))!
                .SetValue(profile, userCredentialsId);

            return profile;
        }

        [Test]
        public async Task Handle_CredentialsNotFound_ReturnsNull()
        {
            var userId = "user-1";

            _credentialsRepo
                .Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<UserCredentials, bool>>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((UserCredentials?)null);

            var result = await _handler.Handle(
                new GetCurrentUserQuery(userId),
                CancellationToken.None);

            Assert.That(result, Is.Null);

            _credentialsRepo.Verify(
                r => r.FindAsync(It.IsAny<Expression<Func<UserCredentials, bool>>>(), It.IsAny<CancellationToken>()),
                Times.Once);

            _profileRepo.Verify(
                r => r.FindAsync(It.IsAny<Expression<Func<UserProfile, bool>>>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Test]
        public async Task Handle_CredentialsFound_ProfileMissing_ReturnsUserDtoWithNullProfileFields()
        {
            var userId = "user-1";
            var creds = CreateCredentials(userId, "user@test.com", UserRole.Admin);

            _credentialsRepo
                .Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<UserCredentials, bool>>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(creds);

            _profileRepo
                .Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<UserProfile, bool>>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((UserProfile?)null);

            var result = await _handler.Handle(
                new GetCurrentUserQuery(userId),
                CancellationToken.None);

            Assert.That(result, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(result!.Id, Is.EqualTo(creds.Id));
                Assert.That(result.Email, Is.EqualTo(creds.Email));
                Assert.That(result.Role, Is.EqualTo(creds.Role));

                Assert.That(result.FirstName, Is.Null);
                Assert.That(result.LastName, Is.Null);
                Assert.That(result.AddressOne, Is.Null);
                Assert.That(result.AddressTwo, Is.Null);
                Assert.That(result.City, Is.Null);
                Assert.That(result.State, Is.Null);
                Assert.That(result.ZipCode, Is.Null);

                Assert.That(result.Skills, Is.Empty);
                Assert.That(result.Preferences, Is.EqualTo(string.Empty));
                Assert.That(result.Availability, Is.Empty);
            });

            _credentialsRepo.Verify(
                r => r.FindAsync(It.IsAny<Expression<Func<UserCredentials, bool>>>(), It.IsAny<CancellationToken>()),
                Times.Once);

            _profileRepo.Verify(
                r => r.FindAsync(It.IsAny<Expression<Func<UserProfile, bool>>>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Test]
        public async Task Handle_CredentialsAndProfileFound_ReturnsUserDtoWithProfileData()
        {
            var userId = "user-1";
            var creds = CreateCredentials(userId, "user@test.com", UserRole.Volunteer);
            var profile = CreateProfile(userId, "Alice", "Smith");

            _credentialsRepo
                .Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<UserCredentials, bool>>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(creds);

            _profileRepo
                .Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<UserProfile, bool>>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(profile);

            var result = await _handler.Handle(
                new GetCurrentUserQuery(userId),
                CancellationToken.None);

            Assert.That(result, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(result!.Id, Is.EqualTo(creds.Id));
                Assert.That(result.Email, Is.EqualTo(creds.Email));
                Assert.That(result.Role, Is.EqualTo(creds.Role));

                Assert.That(result.FirstName, Is.EqualTo(profile.FirstName));
                Assert.That(result.LastName, Is.EqualTo(profile.LastName));

                Assert.That(result.AddressOne, Is.EqualTo(profile.AddressOne));
                Assert.That(result.AddressTwo, Is.EqualTo(profile.AddressTwo));
                Assert.That(result.City, Is.EqualTo(profile.City));
                Assert.That(result.State, Is.EqualTo(profile.State));
                Assert.That(result.ZipCode, Is.EqualTo(profile.ZipCode));

                Assert.That(result.Skills, Is.EquivalentTo(profile.Skills));
                Assert.That(result.Preferences, Is.EqualTo(profile.Preferences));
                Assert.That(result.Availability, Is.EquivalentTo(profile.Availability));
            });

            _credentialsRepo.Verify(
                r => r.FindAsync(It.IsAny<Expression<Func<UserCredentials, bool>>>(), It.IsAny<CancellationToken>()),
                Times.Once);

            _profileRepo.Verify(
                r => r.FindAsync(It.IsAny<Expression<Func<UserProfile, bool>>>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }
}
