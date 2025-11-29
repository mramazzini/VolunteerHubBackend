using System.Linq.Expressions;
using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;
using SixSeven.Application.Dtos;
using SixSeven.Application.Features.Events;
using SixSeven.Application.Features.VolunteerHistory;
using SixSeven.Application.Interfaces.Repositories;
using SixSeven.Controllers;
using SixSeven.Domain.DTO;
using SixSeven.Domain.Entities;
using SixSeven.Domain.Enums;
using SixSeven.Domain.Models;
using EventDto = SixSeven.Application.Dtos.EventDto;

namespace SixSeven.API.Tests.Controllers
{
    [TestFixture]
    public class EventsControllerTests
    {
        private Mock<IMediator> _mediator = null!;
        private Mock<IGenericRepository<Domain.Entities.VolunteerHistory>> _historyRepo = null!;
        private EventsController _controller = null!;

        [SetUp]
        public void SetUp()
        {
            _mediator = new Mock<IMediator>();
            _historyRepo = new Mock<IGenericRepository<Domain.Entities.VolunteerHistory>>();
            _controller = new EventsController(_mediator.Object, _historyRepo.Object);
        }

        [Test]
        public async Task GetUpcoming_ReturnsOkWithEvents_FromMediator()
        {
            var events = new List<EventDto>
            {
                new()
                {
                    Id = "e1",
                    Name = "Event 1",
                    Description = "Desc 1",
                    Location = "Loc 1",
                    DateIsoString = DateTime.UtcNow.AddHours(1).ToString("O"),
                    Urgency = EventUrgency.Low,
                    RequiredSkills = new List<VolunteerSkill> { VolunteerSkill.Cooking }
                },
                new()
                {
                    Id = "e2",
                    Name = "Event 2",
                    Description = "Desc 2",
                    Location = "Loc 2",
                    DateIsoString = DateTime.UtcNow.AddHours(2).ToString("O"),
                    Urgency = EventUrgency.High,
                    RequiredSkills = new List<VolunteerSkill> { VolunteerSkill.Driving }
                }
            };

            _mediator
                .Setup(m => m.Send(It.IsAny<GetUpcomingEventsQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(events);

            var result = await _controller.GetUpcoming();

            Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
            var ok = (OkObjectResult)result.Result!;
            var value = ok.Value as IReadOnlyList<EventDto>;
            Assert.That(value, Is.Not.Null);
            Assert.That(value!.Count, Is.EqualTo(2));

            _mediator.Verify(
                m => m.Send(It.IsAny<GetUpcomingEventsQuery>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Test]
        public async Task GetVolunteerHistory_NoUserId_ReturnsUnauthorized()
        {
            var httpContext = new DefaultHttpContext();
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            var result = await _controller.GetVolunteerHistory();

            Assert.That(result.Result, Is.InstanceOf<UnauthorizedResult>());

            _mediator.Verify(
                m => m.Send(It.IsAny<GetVolunteerHistoryForUserQuery>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Test]
        public async Task GetVolunteerHistory_WithUserId_ReturnsOkWithData()
        {
            var userId = "user-1";
            var identity = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId)
            }, "TestAuth");
            var principal = new ClaimsPrincipal(identity);

            var httpContext = new DefaultHttpContext
            {
                User = principal
            };

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            var histories = new List<VolunteerHistoryDto>
            {
                new()
                {
                    Id = "e1",
                    Name = "Event 1",
                    Description = "D1",
                    Location = "Loc",
                    DateIsoString = DateTime.UtcNow.ToString("O"),
                    Urgency = EventUrgency.Low,
                    RequiredSkills = new List<VolunteerSkill>(),
                    TimeAtEvent = "1 hour"
                }
            };

            _mediator
                .Setup(m => m.Send(It.IsAny<GetVolunteerHistoryForUserQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(histories);

            var result = await _controller.GetVolunteerHistory();

            Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
            var ok = (OkObjectResult)result.Result!;
            var value = ok.Value as IReadOnlyList<VolunteerHistoryDto>;
            Assert.That(value, Is.Not.Null);
            Assert.That(value!.Count, Is.EqualTo(1));

            _mediator.Verify(
                m => m.Send(
                    It.Is<GetVolunteerHistoryForUserQuery>(q => q.UserId == userId),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Test]
        public async Task UpdateEvent_ValidRequest_SendsCommandAndReturnsOk()
        {
            var request = new UpsertEventRequest
            {
                Id = "event-1",
                Name = "Updated",
                Description = "Desc",
                Location = "Loc",
                DateUtc = new DateTime(2025, 1, 1, 10, 0, 0, DateTimeKind.Utc),
                Urgency = "High",
                RequiredSkills = new[] { "Cooking", "Driving" }
            };


            var returnedDto = new EventDto
            {
                Id = "event-1",
                Name = "Updated",
                Description = "Desc",
                Location = "Loc",
                DateIsoString = request.DateUtc.ToString("O"),
                Urgency = EventUrgency.High,
                RequiredSkills = new List<VolunteerSkill> { VolunteerSkill.Cooking, VolunteerSkill.Driving }
            };

            _mediator
                .Setup(m => m.Send(It.IsAny<UpsertEventCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(returnedDto);

            var result = await _controller.UpdateEvent(request, CancellationToken.None);

            Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
            var ok = (OkObjectResult)result.Result!;
            Assert.That(ok.Value, Is.EqualTo(returnedDto));

            _mediator.Verify(
                m => m.Send(
                    It.Is<UpsertEventCommand>(c =>
                        c.Id == request.Id &&
                        c.Name == request.Name &&
                        c.Description == request.Description &&
                        c.Location == request.Location &&
                        c.DateUtc == request.DateUtc &&
                        c.Urgency == request.Urgency &&
                        c.RequiredSkills.SequenceEqual(request.RequiredSkills)),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Test]
        public async Task AssignVolunteer_NoAssignmentFound_ReturnsNotFound()
        {
            var request = new AssignVolunteerRequest
            {
                VolunteerId = "vol-1",
                DurationMinutes = 90
            };

            _mediator
                .Setup(m => m.Send(It.IsAny<AssignVolunteerToEventCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((AssignVolunteerToEventResult?)null);

            var result = await _controller.AssignVolunteer("event-1", request, CancellationToken.None);

            Assert.That(result.Result, Is.InstanceOf<NotFoundResult>());

            _mediator.Verify(
                m => m.Send(
                    It.Is<AssignVolunteerToEventCommand>(c =>
                        c.EventId == "event-1" &&
                        c.VolunteerId == "vol-1" &&
                        c.DurationMinutes == 90),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Test]
        public async Task AssignVolunteer_Success_ReturnsOkWithResult()
        {
            var request = new AssignVolunteerRequest
            {
                VolunteerId = "vol-1",
                DurationMinutes = 90
            };

            var expected = new AssignVolunteerToEventResult(
                VolunteerHistoryId: "vh-1",
                EventId: "event-1",
                VolunteerId: "vol-1",
                DateUtc: new DateTime(2025, 1, 1, 10, 0, 0, DateTimeKind.Utc),
                DurationMinutes: 90);

            _mediator
                .Setup(m => m.Send(It.IsAny<AssignVolunteerToEventCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expected);

            var result = await _controller.AssignVolunteer("event-1", request, CancellationToken.None);

            Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
            var ok = (OkObjectResult)result.Result!;
            Assert.That(ok.Value, Is.EqualTo(expected));

            _mediator.Verify(
                m => m.Send(
                    It.Is<AssignVolunteerToEventCommand>(c =>
                        c.EventId == "event-1" &&
                        c.VolunteerId == "vol-1" &&
                        c.DurationMinutes == 90),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Test]
        public async Task GetAssignedVolunteers_ReturnsDistinctUserIds()
        {
            var eventId = "event-1";

            var h1 = new Domain.Entities.VolunteerHistory(
                userId: "vol-1",
                eventId: eventId,
                dateUtc: new DateTime(2025, 1, 1, 10, 0, 0, DateTimeKind.Utc),
                durationMinutes: 60);

            var h2 = new Domain.Entities.VolunteerHistory(
                userId: "vol-2",
                eventId: eventId,
                dateUtc: new DateTime(2025, 1, 1, 11, 0, 0, DateTimeKind.Utc),
                durationMinutes: 45);

            var h3 = new Domain.Entities.VolunteerHistory(
                userId: "vol-1",
                eventId: eventId,
                dateUtc: new DateTime(2025, 1, 2, 9, 0, 0, DateTimeKind.Utc),
                durationMinutes: 30);

            var data = new List<Domain.Entities.VolunteerHistory> { h1, h2, h3 };

            _historyRepo
                .Setup(r => r.GetAsync(
                    It.IsAny<Expression<Func<Domain.Entities.VolunteerHistory, bool>>>(),
                    It.IsAny<CancellationToken>()))
                .Returns<Expression<Func<Domain.Entities.VolunteerHistory, bool>>, CancellationToken>((predicate, _) =>
                {
                    var filtered = data.AsQueryable().Where(predicate).ToList();
                    return Task.FromResult<IReadOnlyList<Domain.Entities.VolunteerHistory>>(filtered);
                });

            var result = await _controller.GetAssignedVolunteers(eventId, CancellationToken.None);

            Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
            var ok = (OkObjectResult)result.Result!;
            var value = ok.Value as IReadOnlyList<string>;
            Assert.That(value, Is.Not.Null);
            Assert.That(value!.Count, Is.EqualTo(2));
            Assert.That(value, Does.Contain("vol-1"));
            Assert.That(value, Does.Contain("vol-2"));

            _historyRepo.Verify(
                r => r.GetAsync(
                    It.IsAny<Expression<Func<Domain.Entities.VolunteerHistory, bool>>>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }
}
