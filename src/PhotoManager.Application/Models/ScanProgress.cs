namespace PhotoManager.Application.Models;

public enum ScanStage { Discovering, ReadingMetadata, Saving, Completed, Cancelled }

public sealed record ScanProgress(ScanStage Stage, int Discovered, int Processed, int Added, int Skipped, int Failed, string? CurrentFile = null);

public sealed record ScanResult(int Discovered, int Added, int Updated, int Skipped, int Failed, bool Cancelled, IReadOnlyList<string> Errors);
