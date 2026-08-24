using System.Collections.Concurrent;
using System.Threading.Channels;
using PhotoManager.Application.Abstractions;
using PhotoManager.Application.Models;
using PhotoManager.Domain.Entities;

namespace PhotoManager.Application.Services;

public sealed class PhotoScanner(IPhotoCatalog catalog, IFileFingerprintService fingerprints, IImageMetadataReader metadataReader) : IPhotoScanner
{
    private static readonly HashSet<string> Extensions = new(StringComparer.OrdinalIgnoreCase)
    { ".jpg", ".jpeg", ".png", ".webp", ".bmp", ".gif", ".tif", ".tiff", ".heic", ".heif", ".dng", ".cr2", ".cr3", ".nef", ".arw", ".raf", ".orf" };

    public async Task<ScanResult> ScanAsync(string folder, bool recursive, IProgress<ScanProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        var root = Path.GetFullPath(folder);
        if (!Directory.Exists(root)) throw new DirectoryNotFoundException(root);

        var paths = Channel.CreateBounded<string>(new BoundedChannelOptions(256) { SingleWriter = true, FullMode = BoundedChannelFullMode.Wait });
        var drafts = Channel.CreateBounded<PhotoDraft>(new BoundedChannelOptions(128) { FullMode = BoundedChannelFullMode.Wait });
        var errors = new ConcurrentQueue<string>();
        var discovered = 0; var processed = 0; var added = 0; var skipped = 0; var failed = 0;

        var producer = Task.Run(async () =>
        {
            try
            {
                var options = new EnumerationOptions { RecurseSubdirectories = recursive, IgnoreInaccessible = true, ReturnSpecialDirectories = false };
                foreach (var path in Directory.EnumerateFiles(root, "*", options))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (!Extensions.Contains(Path.GetExtension(path))) continue;
                    var found = Interlocked.Increment(ref discovered);
                    // Do not flood the WPF dispatcher with one message per file.
                    if (found == 1 || found % 100 == 0)
                        progress?.Report(new(ScanStage.Discovering, found, processed, added, skipped, failed, path));
                    await paths.Writer.WriteAsync(path, cancellationToken);
                }
            }
            finally { paths.Writer.TryComplete(); }
        }, cancellationToken);

        var workerCount = Math.Clamp(Environment.ProcessorCount / 2, 2, 6);
        var workers = Enumerable.Range(0, workerCount).Select(_ => Task.Run(async () =>
        {
            await foreach (var path in paths.Reader.ReadAllAsync(cancellationToken))
            {
                try
                {
                    var info = new FileInfo(path);
                    var existing = await catalog.FindByPathAsync(info.FullName, cancellationToken);
                    if (existing is not null && existing.FileSize == info.Length && existing.LastWriteUtc == info.LastWriteTimeUtc)
                    {
                        Interlocked.Increment(ref skipped);
                        continue;
                    }
                    var quickHash = await fingerprints.ComputeQuickAsync(path, cancellationToken);
                    var candidate = await catalog.FindFingerprintCandidateAsync(info.Length, quickHash, info.FullName, cancellationToken);
                    string? fullHash = null;
                    if (candidate is not null)
                    {
                        fullHash = await fingerprints.ComputeFullAsync(path, cancellationToken);
                        var candidateHash = candidate.FullHash ?? await fingerprints.ComputeFullAsync(candidate.Path, cancellationToken);
                        if (candidate.FullHash is null) await catalog.SetFullHashAsync(candidate.Id, candidateHash, cancellationToken);
                        if (StringComparer.OrdinalIgnoreCase.Equals(fullHash, candidateHash))
                        {
                            Interlocked.Increment(ref skipped);
                            continue;
                        }
                    }
                    var metadata = await metadataReader.ReadAsync(path, cancellationToken);
                    await drafts.Writer.WriteAsync(new(info.FullName, info.Name, info.Length, info.LastWriteTimeUtc, quickHash, fullHash,
                        metadata.Width, metadata.Height, metadata.CapturedAt, metadata.Camera, metadata.Lens), cancellationToken);
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex)
                {
                    errors.Enqueue($"{path}: {ex.Message}");
                    Interlocked.Increment(ref failed);
                }
                finally
                {
                    var done = Interlocked.Increment(ref processed);
                    if (done == 1 || done % 25 == 0)
                        progress?.Report(new(ScanStage.ReadingMetadata, discovered, done, added, skipped, failed, path));
                }
            }
        }, cancellationToken)).ToArray();

        var writer = Task.Run(async () =>
        {
            var batch = new List<PhotoDraft>(100);
            await foreach (var draft in drafts.Reader.ReadAllAsync(cancellationToken))
            {
                batch.Add(draft);
                if (batch.Count < 100) continue;
                await catalog.UpsertBatchAsync(batch, cancellationToken);
                Interlocked.Add(ref added, batch.Count);
                batch.Clear();
                progress?.Report(new(ScanStage.Saving, discovered, processed, added, skipped, failed));
            }
            if (batch.Count > 0)
            {
                await catalog.UpsertBatchAsync(batch, cancellationToken);
                Interlocked.Add(ref added, batch.Count);
            }
        }, cancellationToken);

        try
        {
            await producer;
            await Task.WhenAll(workers);
            drafts.Writer.TryComplete();
            await writer;
            progress?.Report(new(ScanStage.Completed, discovered, processed, added, skipped, failed));
            return new(discovered, added, 0, skipped, failed, false, errors.ToArray());
        }
        catch (OperationCanceledException)
        {
            paths.Writer.TryComplete(); drafts.Writer.TryComplete();
            progress?.Report(new(ScanStage.Cancelled, discovered, processed, added, skipped, failed));
            return new(discovered, added, 0, skipped, failed, true, errors.ToArray());
        }
    }
}
