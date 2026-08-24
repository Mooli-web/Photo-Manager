using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace PhotoManager.Wpf.Services;

public sealed class ThumbnailService(string cacheFolder) : IThumbnailService
{
    private readonly string _cacheFolder = Create(cacheFolder);
    private readonly SemaphoreSlim _gate = new(4, 4);

    public async Task<ImageSource?> GetAsync(string sourcePath, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            return await Task.Run(() => Load(sourcePath, cancellationToken), cancellationToken);
        }
        finally { _gate.Release(); }
    }

    private ImageSource? Load(string sourcePath, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!File.Exists(sourcePath)) return null;
        var info = new FileInfo(sourcePath);
        var key = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{sourcePath}|{info.Length}|{info.LastWriteTimeUtc.Ticks}"))).ToLowerInvariant();
        var cached = Path.Combine(_cacheFolder, key + ".jpg");
        try
        {
            if (!File.Exists(cached))
            {
                using var stream = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                var decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.IgnoreColorProfile, BitmapCacheOption.OnLoad);
                var source = decoder.Frames[0];
                var scale = Math.Min(1d, 256d / Math.Max(source.PixelWidth, source.PixelHeight));
                var transformed = new TransformedBitmap(source, new ScaleTransform(scale, scale));
                var encoder = new JpegBitmapEncoder { QualityLevel = 82 };
                encoder.Frames.Add(BitmapFrame.Create(transformed));
                using var output = new FileStream(cached + ".tmp", FileMode.Create, FileAccess.Write, FileShare.None);
                encoder.Save(output); output.Close(); File.Move(cached + ".tmp", cached, true);
            }
            using var thumbnail = new FileStream(cached, FileMode.Open, FileAccess.Read, FileShare.Read);
            var image = new BitmapImage(); image.BeginInit(); image.CacheOption = BitmapCacheOption.OnLoad; image.StreamSource = thumbnail; image.EndInit(); image.Freeze(); return image;
        }
        catch (Exception ex) when (ex is IOException or NotSupportedException or FileFormatException or UnauthorizedAccessException) { return null; }
    }

    private static string Create(string path) { Directory.CreateDirectory(path); return path; }
}
