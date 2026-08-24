using PhotoManager.Application.Models;

namespace PhotoManager.Application.Abstractions;

public interface IPhotoScanner
{
    Task<ScanResult> ScanAsync(string folder, bool recursive, IProgress<ScanProgress>? progress = null, CancellationToken cancellationToken = default);
}
