namespace PhotoManager.Domain.Entities;

public sealed record PhotoDraft(
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
    string? Error = null);
