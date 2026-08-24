using System.Buffers;
using System.Security.Cryptography;
using PhotoManager.Application.Abstractions;

namespace PhotoManager.Infrastructure.Files;

public sealed class FileFingerprintService : IFileFingerprintService
{
    private const int SampleSize = 64 * 1024;

    public async ValueTask<string> ComputeQuickAsync(string path, CancellationToken cancellationToken = default)
    {
        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, SampleSize,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        var length = BitConverter.GetBytes(stream.Length);
        hash.AppendData(length);
        var buffer = ArrayPool<byte>.Shared.Rent(SampleSize);
        try
        {
            var read = await stream.ReadAsync(buffer.AsMemory(0, SampleSize), cancellationToken);
            hash.AppendData(buffer, 0, read);
            if (stream.Length > SampleSize)
            {
                stream.Seek(Math.Max(0, stream.Length - SampleSize), SeekOrigin.Begin);
                read = await stream.ReadAsync(buffer.AsMemory(0, SampleSize), cancellationToken);
                hash.AppendData(buffer, 0, read);
            }
            return Convert.ToHexString(hash.GetHashAndReset()).ToLowerInvariant();
        }
        finally { ArrayPool<byte>.Shared.Return(buffer); }
    }

    public async ValueTask<string> ComputeFullAsync(string path, CancellationToken cancellationToken = default)
    {
        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 1024 * 1024,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
        var bytes = await SHA256.HashDataAsync(stream, cancellationToken);
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
