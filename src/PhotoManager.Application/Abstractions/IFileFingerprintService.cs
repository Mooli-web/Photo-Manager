namespace PhotoManager.Application.Abstractions;

public interface IFileFingerprintService
{
    ValueTask<string> ComputeQuickAsync(string path, CancellationToken cancellationToken = default);
    ValueTask<string> ComputeFullAsync(string path, CancellationToken cancellationToken = default);
}
