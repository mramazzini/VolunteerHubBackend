using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SixSeven.Domain.Entities;
using SixSeven.Domain.Enums;

namespace SixSeven.Domain.Tests.Entities
{
    [TestFixture]
    public class UserProfileTests
    {
        private static UserProfile CreateMinimalProfile(
            string userId = "user-123",
            string firstName = "John",
            string lastName = "Doe",
            string addressOne = "123 Main St",
            string city = "Houston",
            string state = "TX",
            string zip = "77001",
            string? preferences = null,
            IEnumerable<VolunteerSkill>? skills = null,
            IEnumerable<string>? availability = null)
        {
            return new UserProfile(
                userId,
                firstName,
                lastName,
                addressOne,
                city,
                state,
                zip,
                preferences,
                skills,
                null,
                availability);
        }

        [Test]
        public void Constructor_ValidArguments_SetsPropertiesCorrectly()
        {
            var userId = "user-123";
            var firstName = "John";
            var lastName = "Doe";
            var addressOne = "123 Main St";
            var city = "Houston";
            var state = "TX";
            var zip = "77001";
            var preferences = "Weekends only";
            var skills = new[] { VolunteerSkill.Cooking, VolunteerSkill.Driving };
            var availability = new[] { "Saturday", "Sunday" };

            var profile = new UserProfile(
                userId,
                firstName,
                lastName,
                addressOne,
                city,
                state,
                zip,
                preferences,
                skills,
                null,
                availability);

            Assert.Multiple(() =>
            {
                Assert.That(profile.Id, Is.EqualTo(userId));
                Assert.That(profile.UserCredentialsId, Is.EqualTo(userId));
                Assert.That(profile.FirstName, Is.EqualTo(firstName));
                Assert.That(profile.LastName, Is.EqualTo(lastName));
                Assert.That(profile.AddressOne, Is.EqualTo(addressOne));
                Assert.That(profile.City, Is.EqualTo(city));
                Assert.That(profile.State, Is.EqualTo(state));
                Assert.That(profile.ZipCode, Is.EqualTo(zip));
                Assert.That(profile.Preferences, Is.EqualTo(preferences));
                Assert.That(profile.Skills, Is.EquivalentTo(skills));
                Assert.That(profile.Availability, Is.EquivalentTo(availability));
            });
        }

        [Test]
        public void Constructor_NullPreferences_SetsEmptyString()
        {
            var profile = CreateMinimalProfile(preferences: null);

            Assert.That(profile.Preferences, Is.EqualTo(string.Empty));
        }

        [Test]
        public void Constructor_NullSkills_SetsEmptyList()
        {
            var profile = CreateMinimalProfile(skills: null);

            Assert.That(profile.Skills, Is.Not.Null);
            Assert.That(profile.Skills, Is.Empty);
        }

        [Test]
        public void Constructor_DuplicateSkills_AreDeduplicated()
        {
            var skills = new[]
            {
                VolunteerSkill.Cooking,
                VolunteerSkill.Cooking,
                VolunteerSkill.Driving
            };

            var profile = CreateMinimalProfile(skills: skills);

            Assert.That(profile.Skills.Count, Is.EqualTo(2));
            Assert.That(profile.Skills, Is.EquivalentTo(new[]
            {
                VolunteerSkill.Cooking,
                VolunteerSkill.Driving
            }));
        }

        [Test]
        public void Constructor_NullAvailability_SetsEmptyList()
        {
            var profile = CreateMinimalProfile(availability: null);

            Assert.That(profile.Availability, Is.Not.Null);
            Assert.That(profile.Availability, Is.Empty);
        }

        [Test]
        public void Constructor_Availability_FiltersNullEmptyAndDeduplicates()
        {
            var availability = new[]
            {
                "Saturday",
                "Sunday",
                "",
                "   ",
                "Saturday"
            };

            var profile = CreateMinimalProfile(availability: availability);

            Assert.That(profile.Availability.Count, Is.EqualTo(2));
            Assert.That(profile.Availability, Is.EquivalentTo(new[] { "Saturday", "Sunday" }));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void Constructor_InvalidUserCredentialsId_ThrowsArgumentException(string? invalid)
        {
            var ex = Assert.Throws<ArgumentException>(() =>
                new UserProfile(
                    invalid!,
                    "John",
                    "Doe",
                    "123 Main St",
                    "Houston",
                    "TX",
                    "77001"));

            Assert.That(ex!.ParamName, Is.EqualTo("userCredentialsId"));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void Constructor_InvalidFirstName_ThrowsArgumentException(string? invalid)
        {
            var ex = Assert.Throws<ArgumentException>(() =>
                new UserProfile(
                    "user-123",
                    invalid!,
                    "Doe",
                    "123 Main St",
                    "Houston",
                    "TX",
                    "77001"));

            Assert.That(ex!.ParamName, Is.EqualTo("firstName"));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void Constructor_InvalidLastName_ThrowsArgumentException(string? invalid)
        {
            var ex = Assert.Throws<ArgumentException>(() =>
                new UserProfile(
                    "user-123",
                    "John",
                    invalid!,
                    "123 Main St",
                    "Houston",
                    "TX",
                    "77001"));

            Assert.That(ex!.ParamName, Is.EqualTo("lastName"));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void Constructor_InvalidAddressOne_ThrowsArgumentException(string? invalid)
        {
            var ex = Assert.Throws<ArgumentException>(() =>
                new UserProfile(
                    "user-123",
                    "John",
                    "Doe",
                    invalid!,
                    "Houston",
                    "TX",
                    "77001"));

            Assert.That(ex!.ParamName, Is.EqualTo("addressOne"));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void Constructor_InvalidCity_ThrowsArgumentException(string? invalid)
        {
            var ex = Assert.Throws<ArgumentException>(() =>
                new UserProfile(
                    "user-123",
                    "John",
                    "Doe",
                    "123 Main St",
                    invalid!,
                    "TX",
                    "77001"));

            Assert.That(ex!.ParamName, Is.EqualTo("city"));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void Constructor_InvalidState_ThrowsArgumentException(string? invalid)
        {
            var ex = Assert.Throws<ArgumentException>(() =>
                new UserProfile(
                    "user-123",
                    "John",
                    "Doe",
                    "123 Main St",
                    "Houston",
                    invalid!,
                    "77001"));

            Assert.That(ex!.ParamName, Is.EqualTo("state"));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void Constructor_InvalidZipCode_ThrowsArgumentException(string? invalid)
        {
            var ex = Assert.Throws<ArgumentException>(() =>
                new UserProfile(
                    "user-123",
                    "John",
                    "Doe",
                    "123 Main St",
                    "Houston",
                    "TX",
                    invalid!));

            Assert.That(ex!.ParamName, Is.EqualTo("zipCode"));
        }

        [Test]
        public void UpdateProfile_ValidArguments_UpdatesAllFields()
        {
            var profile = CreateMinimalProfile(
                firstName: "OldFirst",
                lastName: "OldLast",
                addressOne: "Old Address",
                city: "Old City",
                state: "OS",
                zip: "00000",
                preferences: "Old",
                skills: new[] { VolunteerSkill.Cooking },
                availability: new[] { "Monday" });

            var newSkills = new[] { VolunteerSkill.Driving, VolunteerSkill.Driving };
            var newAvailability = new[] { "Tuesday", "Wednesday", "", "Tuesday" };

            profile.UpdateProfile(
                "NewFirst",
                "NewLast",
                "New Address",
                "Apt 2",
                "New City",
                "NS",
                "99999",
                "New Pref",
                newSkills,
                newAvailability);

            Assert.Multiple(() =>
            {
                Assert.That(profile.FirstName, Is.EqualTo("NewFirst"));
                Assert.That(profile.LastName, Is.EqualTo("NewLast"));
                Assert.That(profile.AddressOne, Is.EqualTo("New Address"));
                Assert.That(profile.AddressTwo, Is.EqualTo("Apt 2"));
                Assert.That(profile.City, Is.EqualTo("New City"));
                Assert.That(profile.State, Is.EqualTo("NS"));
                Assert.That(profile.ZipCode, Is.EqualTo("99999"));
                Assert.That(profile.Preferences, Is.EqualTo("New Pref"));
                Assert.That(profile.Skills, Is.EquivalentTo(new[] { VolunteerSkill.Driving }));
                Assert.That(profile.Availability, Is.EquivalentTo(new[] { "Tuesday", "Wednesday" }));
            });
        }

        [Test]
        public void UpdateProfile_NullPreferences_SetsEmptyString()
        {
            var profile = CreateMinimalProfile(preferences: "Something");

            profile.UpdateProfile(
                "John",
                "Doe",
                "123 Main St",
                null,
                "Houston",
                "TX",
                "77001",
                null,
                null,
                null);

            Assert.That(profile.Preferences, Is.EqualTo(string.Empty));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void UpdateProfile_InvalidFirstName_ThrowsArgumentException(string? invalid)
        {
            var profile = CreateMinimalProfile();

            var ex = Assert.Throws<ArgumentException>(() =>
                profile.UpdateProfile(
                    invalid!,
                    "Doe",
                    "123 Main St",
                    null,
                    "Houston",
                    "TX",
                    "77001",
                    null,
                    null,
                    null));

            Assert.That(ex!.ParamName, Is.EqualTo("firstName"));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void UpdateProfile_InvalidLastName_ThrowsArgumentException(string? invalid)
        {
            var profile = CreateMinimalProfile();

            var ex = Assert.Throws<ArgumentException>(() =>
                profile.UpdateProfile(
                    "John",
                    invalid!,
                    "123 Main St",
                    null,
                    "Houston",
                    "TX",
                    "77001",
                    null,
                    null,
                    null));

            Assert.That(ex!.ParamName, Is.EqualTo("lastName"));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void UpdateProfile_InvalidAddressOne_ThrowsArgumentException(string? invalid)
        {
            var profile = CreateMinimalProfile();

            var ex = Assert.Throws<ArgumentException>(() =>
                profile.UpdateProfile(
                    "John",
                    "Doe",
                    invalid!,
                    null,
                    "Houston",
                    "TX",
                    "77001",
                    null,
                    null,
                    null));

            Assert.That(ex!.ParamName, Is.EqualTo("addressOne"));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void UpdateProfile_InvalidCity_ThrowsArgumentException(string? invalid)
        {
            var profile = CreateMinimalProfile();

            var ex = Assert.Throws<ArgumentException>(() =>
                profile.UpdateProfile(
                    "John",
                    "Doe",
                    "123 Main St",
                    null,
                    invalid!,
                    "TX",
                    "77001",
                    null,
                    null,
                    null));

            Assert.That(ex!.ParamName, Is.EqualTo("city"));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void UpdateProfile_InvalidState_ThrowsArgumentException(string? invalid)
        {
            var profile = CreateMinimalProfile();

            var ex = Assert.Throws<ArgumentException>(() =>
                profile.UpdateProfile(
                    "John",
                    "Doe",
                    "123 Main St",
                    null,
                    "Houston",
                    invalid!,
                    "77001",
                    null,
                    null,
                    null));

            Assert.That(ex!.ParamName, Is.EqualTo("state"));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void UpdateProfile_InvalidZipCode_ThrowsArgumentException(string? invalid)
        {
            var profile = CreateMinimalProfile();

            var ex = Assert.Throws<ArgumentException>(() =>
                profile.UpdateProfile(
                    "John",
                    "Doe",
                    "123 Main St",
                    null,
                    "Houston",
                    "TX",
                    invalid!,
                    null,
                    null,
                    null));

            Assert.That(ex!.ParamName, Is.EqualTo("zipCode"));
        }
    }
}
