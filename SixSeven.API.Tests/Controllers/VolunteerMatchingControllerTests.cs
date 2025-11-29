using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;
using SixSeven.Application.Dtos;
using SixSeven.Application.Features.Matching;
using SixSeven.Controllers;

namespace SixSeven.API.Tests.Controllers
{
    [TestFixture]
    public sealed class VolunteerMatchingControllerTests
    {
        private Mock<IMediator> _mediator = null!;
        private VolunteerMatchingController _controller = null!;

        [SetUp]
        public void SetUp()
        {
            _mediator = new Mock<IMediator>();
            _controller = new VolunteerMatchingController(_mediator.Object);
        }

        [Test]
        public async Task GetVolunteers_ReturnsOkWithResult()
        {
            var volunteers = new List<VolunteerDto>
            {
                new VolunteerDto(
                    Id: "v1",
                    Name: "Alice Smith",
                    Skills: new List<Domain.Enums.VolunteerSkill>(),
                    Availability: new List<string>())
            };

            _mediator
                .Setup(m => m.Send(
                    It.IsAny<GetVolunteersForMatchingQuery>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(volunteers);

            var result = await _controller.GetVolunteers(CancellationToken.None);

            Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
            var ok = (OkObjectResult)result.Result!;
            Assert.That(ok.Value, Is.SameAs(volunteers));

            _mediator.Verify(
                m => m.Send(
                    It.IsAny<GetVolunteersForMatchingQuery>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Test]
        public async Task GetEvents_ReturnsOkWithResult()
        {
            var events = new List<EventDto>
            {
                new EventDto
                {
                    Id = "e1",
                    Name = "Food Drive",
                    Description = "Help sort food",
                    Location = "Center",
                    DateIsoString = DateTime.UtcNow.ToString("O"),
                    Urgency = Domain.Enums.EventUrgency.Medium,
                    RequiredSkills = new List<Domain.Enums.VolunteerSkill>()
                }
            };

            _mediator
                .Setup(m => m.Send(
                    It.IsAny<GetEventsForMatchingQuery>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(events);

            var result = await _controller.GetEvents(CancellationToken.None);

            Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
            var ok = (OkObjectResult)result.Result!;
            Assert.That(ok.Value, Is.SameAs(events));

            _mediator.Verify(
                m => m.Send(
                    It.IsAny<GetEventsForMatchingQuery>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }
}
