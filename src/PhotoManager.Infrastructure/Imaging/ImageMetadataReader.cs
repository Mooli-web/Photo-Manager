using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using PhotoManager.Application.Abstractions;

namespace PhotoManager.Infrastructure.Imaging;

public sealed class ImageMetadataReader : IImageMetadataReader
{
    public ValueTask<ImageMetadata> ReadAsync(string path, CancellationToken cancellationToken = default) =>
        new(Task.Run(() => Read(path, cancellationToken), cancellationToken));

    private static ImageMetadata Read(string path, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            var directories = MetadataExtractor.ImageMetadataReader.ReadMetadata(path);
            cancellationToken.ThrowIfCancellationRequested();
            var exif = directories.OfType<ExifSubIfdDirectory>().FirstOrDefault();
            var ifd = directories.OfType<ExifIfd0Directory>().FirstOrDefault();
            var jpeg = directories.FirstOrDefault(d => d.TryGetInt32(3, out _) && d.TryGetInt32(1, out _));
            int? width = null; int? height = null;
            foreach (var directory in directories)
            {
                if (width is null && directory.TryGetInt32(ExifDirectoryBase.TagExifImageWidth, out var w)) width = w;
                if (height is null && directory.TryGetInt32(ExifDirectoryBase.TagExifImageHeight, out var h)) height = h;
            }
            DateTime? captured = null;
            if (exif?.TryGetDateTime(ExifDirectoryBase.TagDateTimeOriginal, out var date) == true) captured = date;
            var make = ifd?.GetDescription(ExifDirectoryBase.TagMake)?.Trim();
            var model = ifd?.GetDescription(ExifDirectoryBase.TagModel)?.Trim();
            var camera = string.Join(" ", new[] { make, model }.Where(x => !string.IsNullOrWhiteSpace(x))).Trim();
            var lens = exif?.GetDescription(ExifDirectoryBase.TagLensModel)?.Trim();
            return new(width, height, captured, string.IsNullOrWhiteSpace(camera) ? null : camera, lens);
        }
        catch (ImageProcessingException) { return new(null, null, null, null, null); }
        catch (IOException) { return new(null, null, null, null, null); }
    }
}
