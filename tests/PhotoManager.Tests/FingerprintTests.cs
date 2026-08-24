using Xunit;
using PhotoManager.Infrastructure.Files;

namespace PhotoManager.Tests;

public sealed class FingerprintTests
{
    [Fact]
    public async Task Fingerprints_are_stable_and_content_sensitive()
    {
        var folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")); Directory.CreateDirectory(folder);
        try
        {
            var a = Path.Combine(folder, "a.bin"); var b = Path.Combine(folder, "b.bin");
            await File.WriteAllBytesAsync(a, Enumerable.Repeat((byte)1, 200_000).ToArray());
            File.Copy(a, b); var service = new FileFingerprintService();
            Assert.Equal(await service.ComputeQuickAsync(a), await service.ComputeQuickAsync(b));
            Assert.Equal(await service.ComputeFullAsync(a), await service.ComputeFullAsync(b));
            await File.AppendAllTextAsync(b, "changed");
            Assert.NotEqual(await service.ComputeQuickAsync(a), await service.ComputeQuickAsync(b));
        }
        finally { Directory.Delete(folder, true); }
    }
}
