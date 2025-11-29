namespace SixSeven.Domain.Models;

public sealed record FileReportResult(
    string FileName,
    string ContentType,
    byte[] Content);