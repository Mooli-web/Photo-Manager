namespace PhotoManager.Application.Abstractions;

public sealed record ImageMetadata(int? Width, int? Height, DateTime? CapturedAt, string? Camera, string? Lens);

public interface IImageMetadataReader
{
    ValueTask<ImageMetadata> ReadAsync(string path, CancellationToken cancellationToken = default);
}
