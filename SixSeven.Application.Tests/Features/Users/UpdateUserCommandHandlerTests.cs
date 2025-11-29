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
using SixSeven.Domain.DTO;
using SixSeven.Domain.Entities;
using SixSeven.Domain.Enums;

namespace SixSeven.Application.Tests.Features.Users
{
    [TestFixture]
    public class UpdateUserCommandHandlerTests
    {
        private Mock<IGenericRepository<UserCredentials>> _credentialsRepo = null!;
        private Mock<IGenericRepository<UserProfile>> _profileRepo = null!;
        private UpdateUserCommandHandler _handler = null!;

        [SetUp]
        public void SetUp()
        {
            _credentialsRepo = new Mock<IGenericRepository<UserCredentials>>(MockBehavior.Strict);
            _profileRepo = new Mock<IGenericRepository<UserProfile>>(MockBehavior.Strict);
            _handler = new UpdateUserCommandHandler(_credentialsRepo.Object, _profileRepo.Object);
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
            string lastName = "Smith",
            string addressOne = "123 Main",
            string? addressTwo = "Apt 1",
            string city = "City",
            string state = "TX",
            string zip = "77001",
            string preferences = "Pref",
            IReadOnlyCollection<VolunteerSkill>? skills = null,
            IReadOnlyCollection<string>? availability = null)
        {
            var profile = new UserProfile(
                userCredentialsId: userCredentialsId,
                firstName: firstName,
                lastName: lastName,
                addressOne: addressOne,
                city: city,
                state: state,
                zipCode: zip,
                preferences: preferences,
                skills: skills ?? new List<VolunteerSkill> { VolunteerSkill.Cooking },
                otherSkills: null,
                availability: availability ?? new List<string> { "Weekends" });

            typeof(UserProfile).GetProperty(nameof(UserProfile.Id))!
                .SetValue(profile, userCredentialsId);

            if (addressTwo is not null)
            {
                typeof(UserProfile).GetProperty(nameof(UserProfile.AddressTwo))!
                    .SetValue(profile, addressTwo);
            }

            return profile;
        }

        private static UpdateUserRequest CreateRequest(
            string? firstName = null,
            string? lastName = null,
            string? addressOne = null,
            string? addressTwo = null,
            string? city = null,
            string? state = null,
            string? zip = null,
            string? preferences = null,
            IEnumerable<string>? skills = null,
            IReadOnlyList<string>? availability = null)
        {
            return new UpdateUserRequest
            {
                FirstName = firstName,
                LastName = lastName,
                AddressOne = addressOne,
                AddressTwo = addressTwo,
                City = city,
                State = state,
                ZipCode = zip,
                Preferences = preferences,
                Availability = availability,
                Skills = skills
            };
        }

        [Test]
        public void Handle_UserNotFound_ThrowsInvalidOperationException()
        {
            var userId = "user-1";
            var request = CreateRequest(firstName: "Alice");

            _credentialsRepo
                .Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<UserCredentials, bool>>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((UserCredentials?)null);

            var cmd = new UpdateUserCommand(userId, request);

            var ex = Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await _handler.Handle(cmd, CancellationToken.None));

            Assert.That(ex!.Message, Is.EqualTo("User not found."));

            _credentialsRepo.Verify(
                r => r.FindAsync(It.IsAny<Expression<Func<UserCredentials, bool>>>(), It.IsAny<CancellationToken>()),
                Times.Once);

            _profileRepo.Verify(
                r => r.FindAsync(It.IsAny<Expression<Func<UserProfile, bool>>>(), It.IsAny<CancellationToken>()),
                Times.Never);

            _profileRepo.Verify(
                r => r.SaveAsync(It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Test]
        public async Task Handle_ProfileMissing_AllRequiredFieldsProvided_CreatesProfileAndReturnsDto()
        {
            var userId = "user-1";
            var creds = CreateCredentials(userId, "user@test.com", UserRole.Volunteer);

            var request = CreateRequest(
                firstName: "Alice",
                lastName: "Smith",
                addressOne: "456 New St",
                addressTwo: "Unit 2",
                city: "New City",
                state: "CA",
                zip: "90001",
                preferences: "New Pref",
                skills: new[] { "Cooking", "Driving" },
                availability: new[] { "Mon", "Tue" });

            UserProfile? insertedProfile = null;

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

            _profileRepo
                .Setup(r => r.QueueInsert(It.IsAny<UserProfile>(), It.IsAny<CancellationToken>()))
                .Callback<UserProfile, CancellationToken>((p, _) => insertedProfile = p);

            _profileRepo
                .Setup(r => r.SaveAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            var cmd = new UpdateUserCommand(userId, request);

            var result = await _handler.Handle(cmd, CancellationToken.None);

            Assert.That(insertedProfile, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(insertedProfile!.UserCredentialsId, Is.EqualTo(userId));
                Assert.That(insertedProfile.FirstName, Is.EqualTo("Alice"));
                Assert.That(insertedProfile.LastName, Is.EqualTo("Smith"));
                Assert.That(insertedProfile.AddressOne, Is.EqualTo("456 New St"));
                Assert.That(insertedProfile.City, Is.EqualTo("New City"));
                Assert.That(insertedProfile.State, Is.EqualTo("CA"));
                Assert.That(insertedProfile.ZipCode, Is.EqualTo("90001"));
                Assert.That(insertedProfile.Preferences, Is.EqualTo("New Pref"));
                Assert.That(insertedProfile.Skills, Is.EquivalentTo(
                    new[] { VolunteerSkill.Cooking, VolunteerSkill.Driving }));
                Assert.That(insertedProfile.Availability, Is.EquivalentTo(new[] { "Mon", "Tue" }));
            });

            Assert.Multiple(() =>
            {
                Assert.That(result.Id, Is.EqualTo(creds.Id));
                Assert.That(result.Email, Is.EqualTo(creds.Email));
                Assert.That(result.Role, Is.EqualTo(creds.Role));
                Assert.That(result.FirstName, Is.EqualTo(insertedProfile!.FirstName));
                Assert.That(result.LastName, Is.EqualTo(insertedProfile.LastName));
                Assert.That(result.AddressOne, Is.EqualTo(insertedProfile.AddressOne));
                Assert.That(result.City, Is.EqualTo(insertedProfile.City));
                Assert.That(result.State, Is.EqualTo(insertedProfile.State));
                Assert.That(result.ZipCode, Is.EqualTo(insertedProfile.ZipCode));
                Assert.That(result.Skills, Is.EquivalentTo(insertedProfile.Skills));
                Assert.That(result.Preferences, Is.EqualTo(insertedProfile.Preferences));
                Assert.That(result.Availability, Is.EquivalentTo(insertedProfile.Availability));
            });

            _credentialsRepo.Verify(
                r => r.FindAsync(It.IsAny<Expression<Func<UserCredentials, bool>>>(), It.IsAny<CancellationToken>()),
                Times.Once);

            _profileRepo.Verify(
                r => r.FindAsync(It.IsAny<Expression<Func<UserProfile, bool>>>(), It.IsAny<CancellationToken>()),
                Times.Once);

            _profileRepo.Verify(
                r => r.QueueInsert(It.IsAny<UserProfile>(), It.IsAny<CancellationToken>()),
                Times.Once);

            _profileRepo.Verify(
                r => r.SaveAsync(It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Test]
        public void Handle_ProfileMissing_MissingRequiredFields_ThrowsInvalidOperationException()
        {
            var userId = "user-1";
            var creds = CreateCredentials(userId, "user@test.com", UserRole.Volunteer);

            var request = CreateRequest(
                firstName: null,
                lastName: "Smith",
                addressOne: "456 New St",
                city: "New City",
                state: "CA",
                zip: "90001");

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

            var cmd = new UpdateUserCommand(userId, request);

            var ex = Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await _handler.Handle(cmd, CancellationToken.None));

            Assert.That(ex!.Message, Is.EqualTo("Cannot create profile: missing required fields."));

            _credentialsRepo.Verify(
                r => r.FindAsync(It.IsAny<Expression<Func<UserCredentials, bool>>>(), It.IsAny<CancellationToken>()),
                Times.Once);

            _profileRepo.Verify(
                r => r.FindAsync(It.IsAny<Expression<Func<UserProfile, bool>>>(), It.IsAny<CancellationToken>()),
                Times.Once);

            _profileRepo.Verify(
                r => r.QueueInsert(It.IsAny<UserProfile>(), It.IsAny<CancellationToken>()),
                Times.Never);

            _profileRepo.Verify(
                r => r.SaveAsync(It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Test]
        public async Task Handle_ProfileExists_UsesDefaultsWhenFieldsNull_UpdatesProfile()
        {
            var userId = "user-1";
            var creds = CreateCredentials(userId, "user@test.com", UserRole.Volunteer);

            var existingProfile = CreateProfile(
                userCredentialsId: userId,
                firstName: "Original",
                lastName: "Name",
                addressOne: "123 Main",
                addressTwo: "Apt 1",
                city: "Old City",
                state: "TX",
                zip: "77001",
                preferences: "Old Pref",
                skills: new[] { VolunteerSkill.Cooking },
                availability: new[] { "Weekends" });

            var request = CreateRequest(
                firstName: null,
                lastName: "UpdatedLast",
                addressOne: null,
                addressTwo: "New Apt",
                city: "New City",
                state: null,
                zip: null,
                preferences: "New Pref",
                skills: new[] { "Driving" },
                availability: new[] { "Weekdays" });

            _credentialsRepo
                .Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<UserCredentials, bool>>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(creds);

            _profileRepo
                .Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<UserProfile, bool>>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingProfile);

            _profileRepo
                .Setup(r => r.SaveAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            var cmd = new UpdateUserCommand(userId, request);

            var result = await _handler.Handle(cmd, CancellationToken.None);

            Assert.Multiple(() =>
            {
                Assert.That(existingProfile.FirstName, Is.EqualTo("Original"));
                Assert.That(existingProfile.LastName, Is.EqualTo("UpdatedLast"));
                Assert.That(existingProfile.AddressOne, Is.EqualTo("123 Main"));
                Assert.That(existingProfile.AddressTwo, Is.EqualTo("New Apt"));
                Assert.That(existingProfile.City, Is.EqualTo("New City"));
                Assert.That(existingProfile.State, Is.EqualTo("TX"));
                Assert.That(existingProfile.ZipCode, Is.EqualTo("77001"));
                Assert.That(existingProfile.Preferences, Is.EqualTo("New Pref"));
                Assert.That(existingProfile.Skills, Is.EquivalentTo(new[] { VolunteerSkill.Driving }));
                Assert.That(existingProfile.Availability, Is.EquivalentTo(new[] { "Weekdays" }));
            });

            Assert.Multiple(() =>
            {
                Assert.That(result.Id, Is.EqualTo(creds.Id));
                Assert.That(result.Email, Is.EqualTo(creds.Email));
                Assert.That(result.Role, Is.EqualTo(creds.Role));
                Assert.That(result.FirstName, Is.EqualTo(existingProfile.FirstName));
                Assert.That(result.LastName, Is.EqualTo(existingProfile.LastName));
                Assert.That(result.AddressOne, Is.EqualTo(existingProfile.AddressOne));
                Assert.That(result.AddressTwo, Is.EqualTo(existingProfile.AddressTwo));
                Assert.That(result.City, Is.EqualTo(existingProfile.City));
                Assert.That(result.State, Is.EqualTo(existingProfile.State));
                Assert.That(result.ZipCode, Is.EqualTo(existingProfile.ZipCode));
                Assert.That(result.Skills, Is.EquivalentTo(existingProfile.Skills));
                Assert.That(result.Preferences, Is.EqualTo(existingProfile.Preferences));
                Assert.That(result.Availability, Is.EquivalentTo(existingProfile.Availability));
            });

            _credentialsRepo.Verify(
                r => r.FindAsync(It.IsAny<Expression<Func<UserCredentials, bool>>>(), It.IsAny<CancellationToken>()),
                Times.Once);

            _profileRepo.Verify(
                r => r.FindAsync(It.IsAny<Expression<Func<UserProfile, bool>>>(), It.IsAny<CancellationToken>()),
                Times.Once);

            _profileRepo.Verify(
                r => r.SaveAsync(It.IsAny<CancellationToken>()),
                Times.Once);

            _profileRepo.Verify(
                r => r.QueueInsert(It.IsAny<UserProfile>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Test]
        public void Handle_InvalidSkill_ThrowsInvalidOperationException()
        {
            var userId = "user-1";
            var creds = CreateCredentials(userId, "user@test.com", UserRole.Volunteer);
            var existingProfile = CreateProfile(userId);

            var request = CreateRequest(
                skills: new[] { "Cooking", "NotASkill" });

            _credentialsRepo
                .Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<UserCredentials, bool>>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(creds);

            _profileRepo
                .Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<UserProfile, bool>>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingProfile);

            var cmd = new UpdateUserCommand(userId, request);

            var ex = Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await _handler.Handle(cmd, CancellationToken.None));

            Assert.That(ex!.Message, Does.Contain("Invalid skill: 'NotASkill'"));

            _profileRepo.Verify(
                r => r.SaveAsync(It.IsAny<CancellationToken>()),
                Times.Never);
        }
    }
}
