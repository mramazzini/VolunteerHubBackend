using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using SixSeven.Application.Dtos;
using SixSeven.Application.Features.Matching;
using SixSeven.Application.Interfaces.Repositories;
using SixSeven.Domain.Entities;
using SixSeven.Domain.Enums;

namespace SixSeven.Application.Tests.Features.Matching
{
    [TestFixture]
    public class GetVolunteersForMatchingHandlerTests
    {
        private Mock<IGenericRepository<UserProfile>> _usersRepo = null!;
        private GetVolunteersForMatchingHandler _handler = null!;

        [SetUp]
        public void SetUp()
        {
            _usersRepo = new Mock<IGenericRepository<UserProfile>>(MockBehavior.Strict);
            _handler = new GetVolunteersForMatchingHandler(_usersRepo.Object);
        }

        private static UserCredentials CreateCredentials(string id, string email, UserRole role)
        {
            var creds = new UserCredentials(email, "hash", role);
            typeof(UserCredentials).GetProperty(nameof(UserCredentials.Id))!
                .SetValue(creds, id);
            return creds;
        }

        private static UserProfile CreateProfile(
            UserCredentials credentials,
            string firstName,
            string lastName,
            IEnumerable<VolunteerSkill> skills,
            IEnumerable<string> availability)
        {
            var profile = new UserProfile(
                userCredentialsId: credentials.Id,
                firstName: firstName,
                lastName: lastName,
                addressOne: "123 Main St",
                city: "City",
                state: "TX",
                zipCode: "77001",
                preferences: null,
                skills: skills,
                otherSkills: null,
                availability: availability);

            typeof(UserProfile).GetProperty(nameof(UserProfile.Credentials))!
                .SetValue(profile, credentials);

            return profile;
        }

        [Test]
        public async Task Handle_ReturnsOnlyVolunteerProfiles_MappedToVolunteerDto()
        {
            var volunteerCreds = CreateCredentials("vol-1", "vol@test.com", UserRole.Volunteer);
            var adminCreds = CreateCredentials("admin-1", "admin@test.com", UserRole.Admin);

            var volunteerSkills = new List<VolunteerSkill> { VolunteerSkill.Cooking, VolunteerSkill.Driving };
            var volunteerAvailability = new List<string> { "Mon Morning", "Fri Evening" };

            var volunteerProfile = CreateProfile(
                volunteerCreds,
                "Alice",
                "Smith",
                volunteerSkills,
                volunteerAvailability);

            var adminProfile = CreateProfile(
                adminCreds,
                "Bob",
                "Admin",
                new List<VolunteerSkill> { VolunteerSkill.Teaching },
                new List<string> { "Tue Afternoon" });

            var allProfiles = new List<UserProfile> { volunteerProfile, adminProfile }.AsQueryable();

            _usersRepo
                .Setup(r => r.GetAsync(
                    It.IsAny<Expression<Func<UserProfile, bool>>>(),
                    It.IsAny<CancellationToken>()))
                .Returns<Expression<Func<UserProfile, bool>>, CancellationToken>((predicate, _) =>
                {
                    var filtered = allProfiles.Where(predicate).ToList();
                    return Task.FromResult<IReadOnlyList<UserProfile>>(filtered);
                });

            var result = await _handler.Handle(
                new GetVolunteersForMatchingQuery(),
                CancellationToken.None);
        }
    }
}
