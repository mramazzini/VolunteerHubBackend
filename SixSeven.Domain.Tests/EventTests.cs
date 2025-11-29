using NUnit.Framework;
using SixSeven.Domain.Entities;
using SixSeven.Domain.Enums;

namespace SixSeven.Domain.Tests
{
    [TestFixture]
    public class EventTests
    {
        [Test]
        public void Constructor_ValidArguments_SetsPropertiesCorrectly()
        {
            var name = "Food Drive";
            var description = "Collect canned food for local families.";
            var location = "Community Center";
            var date = new DateTime(2025, 1, 1, 12, 0, 0, DateTimeKind.Unspecified);
            var urgency = EventUrgency.High;

            var skills = new[]
            {
                VolunteerSkill.Cooking,
                VolunteerSkill.Fundraising,
                VolunteerSkill.EventPlanning
            };

            var ev = new Event(name, description, location, date, urgency, skills);

            Assert.That(ev.Id, Is.Not.Null.And.Not.Empty);
            Assert.That(ev.Name, Is.EqualTo(name));
            Assert.That(ev.Description, Is.EqualTo(description));
            Assert.That(ev.Location, Is.EqualTo(location));

            Assert.That(ev.DateUtc, Is.EqualTo(date));
            Assert.That(ev.DateUtc.Kind, Is.EqualTo(DateTimeKind.Utc));

            Assert.That(ev.Urgency, Is.EqualTo(urgency));
            Assert.That(ev.RequiredSkills, Is.EquivalentTo(skills));
        }

        [Test]
        public void Constructor_SetsDateKindToUtc_WhenLocalOrUnspecifiedProvided()
        {
            var localDate = new DateTime(2025, 1, 1, 8, 0, 0, DateTimeKind.Local);

            var ev = new Event(
                "Test Event",
                "Description",
                "Location",
                localDate,
                EventUrgency.Low,
                Enumerable.Empty<VolunteerSkill>());

            Assert.That(ev.DateUtc, Is.EqualTo(localDate));
            Assert.That(ev.DateUtc.Kind, Is.EqualTo(DateTimeKind.Utc));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void Constructor_InvalidName_ThrowsArgumentException(string? invalidName)
        {
            var ex = Assert.Throws<ArgumentException>(() =>
                new Event(
                    invalidName!,
                    "Valid description",
                    "Valid location",
                    DateTime.UtcNow,
                    EventUrgency.Low,
                    Enumerable.Empty<VolunteerSkill>()));

            Assert.That(ex!.ParamName, Is.EqualTo("name"));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void Constructor_InvalidDescription_ThrowsArgumentException(string? invalidDescription)
        {
            var ex = Assert.Throws<ArgumentException>(() =>
                new Event(
                    "Valid name",
                    invalidDescription!,
                    "Valid location",
                    DateTime.UtcNow,
                    EventUrgency.Low,
                    Enumerable.Empty<VolunteerSkill>()));

            Assert.That(ex!.ParamName, Is.EqualTo("description"));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void Constructor_InvalidLocation_ThrowsArgumentException(string? invalidLocation)
        {
            var ex = Assert.Throws<ArgumentException>(() =>
                new Event(
                    "Valid name",
                    "Valid description",
                    invalidLocation!,
                    DateTime.UtcNow,
                    EventUrgency.Low,
                    Enumerable.Empty<VolunteerSkill>()));

            Assert.That(ex!.ParamName, Is.EqualTo("location"));
        }

        [Test]
        public void Constructor_NullRequiredSkills_SetsEmptyList()
        {
            var ev = new Event(
                "Name",
                "Desc",
                "Location",
                DateTime.UtcNow,
                EventUrgency.Medium,
                null!);

            Assert.That(ev.RequiredSkills, Is.Not.Null);
            Assert.That(ev.RequiredSkills, Is.Empty);
        }

        [Test]
        public void Constructor_DuplicateSkills_AreDeduplicatedAndCopied()
        {
            var inputSkills = new List<VolunteerSkill>
            {
                VolunteerSkill.Cooking,
                VolunteerSkill.Cooking,
                VolunteerSkill.Marketing
            };

            var ev = new Event(
                "Name",
                "Desc",
                "Location",
                DateTime.UtcNow,
                EventUrgency.Medium,
                inputSkills);

            inputSkills.Clear();

            Assert.That(ev.RequiredSkills.Count, Is.EqualTo(2));
            Assert.That(ev.RequiredSkills, Does.Contain(VolunteerSkill.Cooking));
            Assert.That(ev.RequiredSkills, Does.Contain(VolunteerSkill.Marketing));
        }

        [Test]
        public void Reschedule_SetsDateAndKindToUtc()
        {
            var originalDate = new DateTime(2025, 1, 1, 12, 0, 0, DateTimeKind.Utc);
            var newDate = new DateTime(2025, 2, 1, 9, 30, 0, DateTimeKind.Unspecified);

            var ev = new Event(
                "Name",
                "Desc",
                "Location",
                originalDate,
                EventUrgency.Low,
                Enumerable.Empty<VolunteerSkill>());

            ev.Reschedule(newDate);

            Assert.That(ev.DateUtc, Is.EqualTo(newDate));
            Assert.That(ev.DateUtc.Kind, Is.EqualTo(DateTimeKind.Utc));
        }

        [Test]
        public void UpdateDetails_ValidArguments_UpdatesProperties()
        {
            var ev = new Event(
                "Old Name",
                "Old Desc",
                "Old Location",
                DateTime.UtcNow,
                EventUrgency.Low,
                Enumerable.Empty<VolunteerSkill>());

            var newName = "New Name";
            var newDesc = "New Description";
            var newLocation = "New Location";
            var newUrgency = EventUrgency.High;

            ev.UpdateDetails(newName, newDesc, newLocation, newUrgency);

            Assert.That(ev.Name, Is.EqualTo(newName));
            Assert.That(ev.Description, Is.EqualTo(newDesc));
            Assert.That(ev.Location, Is.EqualTo(newLocation));
            Assert.That(ev.Urgency, Is.EqualTo(newUrgency));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void UpdateDetails_InvalidName_ThrowsArgumentException(string? invalidName)
        {
            var ev = new Event(
                "Name",
                "Desc",
                "Location",
                DateTime.UtcNow,
                EventUrgency.Low,
                Enumerable.Empty<VolunteerSkill>());

            var ex = Assert.Throws<ArgumentException>(() =>
                ev.UpdateDetails(invalidName!, "Desc", "Location", EventUrgency.High));

            Assert.That(ex!.ParamName, Is.EqualTo("name"));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void UpdateDetails_InvalidDescription_ThrowsArgumentException(string? invalidDescription)
        {
            var ev = new Event(
                "Name",
                "Desc",
                "Location",
                DateTime.UtcNow,
                EventUrgency.Low,
                Enumerable.Empty<VolunteerSkill>());

            var ex = Assert.Throws<ArgumentException>(() =>
                ev.UpdateDetails("Name", invalidDescription!, "Location", EventUrgency.High));

            Assert.That(ex!.ParamName, Is.EqualTo("description"));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void UpdateDetails_InvalidLocation_ThrowsArgumentException(string? invalidLocation)
        {
            var ev = new Event(
                "Name",
                "Desc",
                "Location",
                DateTime.UtcNow,
                EventUrgency.Low,
                Enumerable.Empty<VolunteerSkill>());

            var ex = Assert.Throws<ArgumentException>(() =>
                ev.UpdateDetails("Name", "Desc", invalidLocation!, EventUrgency.High));

            Assert.That(ex!.ParamName, Is.EqualTo("location"));
        }

        [Test]
        public void SetRequiredSkills_Null_SetsEmptyList()
        {
            var ev = new Event(
                "Name",
                "Desc",
                "Location",
                DateTime.UtcNow,
                EventUrgency.Low,
                new[] { VolunteerSkill.Driving });

            ev.SetRequiredSkills(null!);

            Assert.That(ev.RequiredSkills, Is.Not.Null);
            Assert.That(ev.RequiredSkills, Is.Empty);
        }

        [Test]
        public void SetRequiredSkills_Duplicates_AreDeduplicatedAndCopied()
        {
            var ev = new Event(
                "Name",
                "Desc",
                "Location",
                DateTime.UtcNow,
                EventUrgency.Low,
                Enumerable.Empty<VolunteerSkill>());

            var skills = new List<VolunteerSkill>
            {
                VolunteerSkill.ITSupport,
                VolunteerSkill.ITSupport,
                VolunteerSkill.Photography
            };

            ev.SetRequiredSkills(skills);

            skills.Clear();

            Assert.That(ev.RequiredSkills.Count, Is.EqualTo(2));
            Assert.That(ev.RequiredSkills, Is.EquivalentTo(new[]
            {
                VolunteerSkill.ITSupport,
                VolunteerSkill.Photography
            }));
        }
    }
}
