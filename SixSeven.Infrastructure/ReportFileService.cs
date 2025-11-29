using System.Globalization;
using System.Text;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SixSeven.Application.Interfaces.Services;
using SixSeven.Domain.DTO;
using SixSeven.Domain.Enums;
using SixSeven.Domain.Models;

namespace SixSeven.Infrastructure;

public sealed class FileReportService : IFileReportService
{
    public Task<FileReportResult> GenerateVolunteerActivityReportAsync(
        IReadOnlyList<VolunteerActivityRowDto> rows,
        ReportFileFormat format,
        CancellationToken cancellationToken = default)
    {
        return format switch
        {
            ReportFileFormat.Csv => Task.FromResult(GenerateVolunteerActivityCsv(rows)),
            ReportFileFormat.Pdf => Task.FromResult(GenerateVolunteerActivityPdf(rows)),
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Unsupported format")
        };
    }

    public Task<FileReportResult> GenerateEventAssignmentsReportAsync(
        IReadOnlyList<EventAssignmentReportDto> reports,
        ReportFileFormat format,
        CancellationToken cancellationToken = default)
    {
        return format switch
        {
            ReportFileFormat.Csv => Task.FromResult(GenerateEventAssignmentsCsv(reports)),
            ReportFileFormat.Pdf => Task.FromResult(GenerateEventAssignmentsPdf(reports)),
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Unsupported format")
        };
    }

    private static FileReportResult GenerateVolunteerActivityCsv(IReadOnlyList<VolunteerActivityRowDto> rows)
    {
        var sb = new StringBuilder();

        sb.AppendLine("UserId,FullName,Email,EventId,EventName,EventDateUtc,DurationMinutes");

        foreach (var r in rows)
        {
            sb.AppendLine(string.Join(",",
                Escape(r.UserId),
                Escape(r.FullName),
                Escape(r.Email),
                Escape(r.EventId),
                Escape(r.EventName),
                Escape(r.EventDateUtc.ToString("O")),
                r.DurationMinutes.ToString()));
        }

        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        return new FileReportResult(
            FileName: $"volunteer-activity-{DateTime.UtcNow:yyyyMMddHHmmss}.csv",
            ContentType: "text/csv",
            Content: bytes);
    }

    private static FileReportResult GenerateEventAssignmentsCsv(IReadOnlyList<EventAssignmentReportDto> reports)
    {
        var sb = new StringBuilder();

        sb.AppendLine("EventId,EventName,EventDateUtc,Location,Urgency,RequiredSkills,UserId,FullName,Email,ParticipationDateUtc,DurationMinutes");

        foreach (var ev in reports)
        {
            var skillsJoined = string.Join(";", ev.RequiredSkills);

            if (ev.Volunteers.Count == 0)
            {
                sb.AppendLine(string.Join(",",
                    Escape(ev.EventId),
                    Escape(ev.EventName),
                    Escape(ev.EventDateUtc.ToString("O")),
                    Escape(ev.Location),
                    Escape(ev.Urgency),
                    Escape(skillsJoined),
                    "", "", "", "", "" 
                ));
                continue;
            }

            foreach (var v in ev.Volunteers)
            {
                sb.AppendLine(string.Join(",",
                    Escape(ev.EventId),
                    Escape(ev.EventName),
                    Escape(ev.EventDateUtc.ToString("O")),
                    Escape(ev.Location),
                    Escape(ev.Urgency),
                    Escape(skillsJoined),
                    Escape(v.UserId),
                    Escape(v.FullName),
                    Escape(v.Email),
                    Escape(v.ParticipationDateUtc.ToString("O")),
                    v.DurationMinutes.ToString()
                ));
            }
        }

        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        return new FileReportResult(
            FileName: $"event-assignments-{DateTime.UtcNow:yyyyMMddHHmmss}.csv",
            ContentType: "text/csv",
            Content: bytes);
    }

    private static FileReportResult GenerateVolunteerActivityPdf(
        IReadOnlyList<VolunteerActivityRowDto> rows)
    {
        var culture = CultureInfo.InvariantCulture;

        var sortedRows = rows
            .OrderBy(r => r.EventDateUtc)
            .ThenBy(r => r.EventName)
            .ThenBy(r => r.FullName)
            .ToList();
        QuestPDF.Settings.License = LicenseType.Community; 

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header()
                    .AlignCenter()
                    .Text("Volunteer Activity Report")
                    .FontSize(18)
                    .SemiBold();

                page.Content().PaddingVertical(10).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(2); // Name
                        columns.RelativeColumn(3); // Email
                        columns.RelativeColumn(3); // Event
                        columns.RelativeColumn(2); // Date
                        columns.RelativeColumn(1); // Minutes
                    });

                    table.Header(header =>
                    {
                        header.Cell().Element(HeaderCell).Text("Volunteer");
                        header.Cell().Element(HeaderCell).Text("Email");
                        header.Cell().Element(HeaderCell).Text("Event");
                        header.Cell().Element(HeaderCell).Text("Date (UTC)");
                        header.Cell().Element(HeaderCell).Text("Minutes");
                    });

                    foreach (var r in sortedRows)
                    {
                        table.Cell().Element(BodyCell).Text(r.FullName);
                        table.Cell().Element(BodyCell).Text(r.Email);
                        table.Cell().Element(BodyCell).Text(r.EventName);
                        table.Cell().Element(BodyCell)
                            .Text(r.EventDateUtc.ToString("yyyy-MM-dd HH:mm", culture));
                        table.Cell().Element(BodyCell)
                            .AlignRight()
                            .Text(r.DurationMinutes.ToString(culture));
                    }
                });

                page.Footer()
                    .AlignRight()
                    .Text($"Generated at: {DateTime.UtcNow.ToString("O", culture)}")
                    .FontSize(8)
                    .FontColor(Colors.Grey.Darken2);
            });
        });

        var bytes = document.GeneratePdf();

        return new FileReportResult(
            FileName: $"volunteer-activity-{DateTime.UtcNow:yyyyMMddHHmmss}.pdf",
            ContentType: "application/pdf",
            Content: bytes);
    }

    private static FileReportResult GenerateEventAssignmentsPdf(
        IReadOnlyList<EventAssignmentReportDto> reports)
    {
        var culture = CultureInfo.InvariantCulture;

        var orderedEvents = reports
            .OrderBy(ev => ev.EventDateUtc)
            .ThenBy(ev => ev.EventName)
            .ToList();
        QuestPDF.Settings.License = LicenseType.Community; 

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header()
                    .AlignCenter()
                    .Text("Event Assignments Report")
                    .FontSize(18)
                    .SemiBold();

                page.Content().PaddingVertical(10).Column(column =>
                {
                    foreach (var ev in orderedEvents)
                    {
                        column.Item().Element(eventContainer =>
                        {
                            eventContainer.PaddingBottom(15).Column(block =>
                            {
                                block.Item().Text($"{ev.EventName}")
                                    .FontSize(14)
                                    .SemiBold();

                                block.Item().Text(
                                        $"{ev.EventDateUtc.ToString("yyyy-MM-dd HH:mm", culture)} UTC  •  {ev.Location}  •  {ev.Urgency}")
                                    .FontSize(10)
                                    .FontColor(Colors.Grey.Darken2);

                                if (ev.RequiredSkills.Any())
                                {
                                    block.Item().Text($"Skills: {string.Join(", ", ev.RequiredSkills)}")
                                        .FontSize(10);
                                }

                                if (ev.Volunteers.Count == 0)
                                {
                                    block.Item().Text("No volunteers recorded.")
                                        .FontSize(10)
                                        .FontColor(Colors.Grey.Darken1);
                                }
                                else
                                {
                                    block.Item().PaddingTop(5).Table(table =>
                                    {
                                        table.ColumnsDefinition(columns =>
                                        {
                                            columns.RelativeColumn(2);
                                            columns.RelativeColumn(3);
                                            columns.RelativeColumn(2);
                                            columns.RelativeColumn(1); 
                                        });

                                        table.Header(header =>
                                        {
                                            header.Cell().Element(HeaderCell).Text("Volunteer");
                                            header.Cell().Element(HeaderCell).Text("Email");
                                            header.Cell().Element(HeaderCell).Text("Date (UTC)");
                                            header.Cell().Element(HeaderCell).Text("Minutes");
                                        });

                                        foreach (var v in ev.Volunteers
                                                     .OrderBy(v => v.ParticipationDateUtc)
                                                     .ThenBy(v => v.FullName))
                                        {
                                            table.Cell().Element(BodyCell).Text(v.FullName);
                                            table.Cell().Element(BodyCell).Text(v.Email);
                                            table.Cell().Element(BodyCell)
                                                .Text(v.ParticipationDateUtc.ToString("yyyy-MM-dd HH:mm", culture));
                                            table.Cell().Element(BodyCell)
                                                .AlignRight()
                                                .Text(v.DurationMinutes.ToString(culture));
                                        }
                                    });
                                }
                            });
                        });
                    }
                });

                page.Footer()
                    .AlignRight()
                    .Text($"Generated at: {DateTime.UtcNow.ToString("O", culture)}")
                    .FontSize(8)
                    .FontColor(Colors.Grey.Darken2);
            });
        });

        var bytes = document.GeneratePdf();

        return new FileReportResult(
            FileName: $"event-assignments-{DateTime.UtcNow:yyyyMMddHHmmss}.pdf",
            ContentType: "application/pdf",
            Content: bytes);
    }

    private static string Escape(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        var escaped = value.Replace("\"", "\"\"");
        return $"\"{escaped}\"";
    }

    private static IContainer HeaderCell(IContainer container) =>
        container
            .PaddingBottom(4)
            .BorderBottom(1)
            .BorderColor(Colors.Grey.Darken2)
            .DefaultTextStyle(x => x.SemiBold());

    private static IContainer BodyCell(IContainer container) =>
        container
            .PaddingVertical(2);
}
