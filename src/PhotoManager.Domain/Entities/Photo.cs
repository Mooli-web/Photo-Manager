namespace PhotoManager.Domain.Entities;

public sealed record Photo(
    long Id,
    string Path,
    string FileName,
    long FileSize,
    DateTime LastWriteUtc,
    string QuickHash,
    string? FullHash,
    int? Width,
    int? Height,
    DateTime? CapturedAt,
    string? Camera,
    string? Lens,
    int Rating,
    string Notes,
    bool IsMissing,
    DateTime IndexedAtUtc);
