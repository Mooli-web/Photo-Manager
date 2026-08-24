using Xunit;
using PhotoManager.Application.Services;
using PhotoManager.Infrastructure.Data;
using PhotoManager.Infrastructure.Files;
using PhotoManager.Infrastructure.Imaging;

namespace PhotoManager.Tests;

public sealed class ScannerTests
{
    [Fact]
    public async Task Scanner_batches_files_and_skips_unchanged_second_scan()
    {
        var folder = Path.Combine(Path.GetTempPath(), "PhotoManagerScan", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            for (var i = 0; i < 120; i++)
                await File.WriteAllBytesAsync(Path.Combine(folder, $"photo-{i:D3}.jpg"), Enumerable.Repeat((byte)i, i + 32).ToArray());
            var catalog = new SqlitePhotoCatalog(Path.Combine(folder, "catalog.sqlite3"));
            await catalog.InitializeAsync();
            var scanner = new PhotoScanner(catalog, new FileFingerprintService(), new ImageMetadataReader());
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            var first = await scanner.ScanAsync(folder, true, cancellationToken: timeout.Token);
            Assert.Equal(120, first.Added);
            Assert.Equal(0, first.Failed);
            var second = await scanner.ScanAsync(folder, true, cancellationToken: timeout.Token);
            Assert.Equal(0, second.Added);
            Assert.Equal(120, second.Skipped);
        }
        finally { try { Directory.Delete(folder, true); } catch (IOException) { } }
    }
}
