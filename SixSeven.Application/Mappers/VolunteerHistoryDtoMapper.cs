using SixSeven.Application.Dtos;
using SixSeven.Domain.DTO;
using SixSeven.Domain.Entities;

namespace SixSeven.Application.Mappers;

public static class VolunteerHistoryDtoMapper
{
    public static VolunteerHistoryDto ToDto(
        this VolunteerHistory history,
        Event ev)
    {
        ArgumentNullException.ThrowIfNull(history);
        ArgumentNullException.ThrowIfNull(ev);

        return new VolunteerHistoryDto
        {
            Id = ev.Id,
            Name = ev.Name,
            Description = ev.Description,
            Location = ev.Location,

            DateIsoString = history.DateUtc.ToString("O"),
            Urgency = ev.Urgency,
            RequiredSkills = ev.RequiredSkills,

            TimeAtEvent = FormatDuration(history.DurationMinutes)
        };
    }

    private static string FormatDuration(int durationMinutes)
    {
        if (durationMinutes <= 0)
            return "0 minutes";

        if (durationMinutes < 60)
            return $"{durationMinutes} minute{(durationMinutes == 1 ? "" : "s")}";

        var hours = durationMinutes / 60;
        var minutes = durationMinutes % 60;

        if (minutes == 0)
            return $"{hours} hour{(hours == 1 ? "" : "s")}";

        return $"{hours} hour{(hours == 1 ? "" : "s")} " +
               $"{minutes} minute{(minutes == 1 ? "" : "s")}";
    }
}