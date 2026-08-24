using PhotoManager.Application.Models;
using PhotoManager.Domain.Entities;

namespace PhotoManager.Application.Abstractions;

public interface IPhotoCatalog
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Photo>> QueryAsync(PhotoQuery query, CancellationToken cancellationToken = default);
    Task<int> CountAsync(PhotoQuery query, CancellationToken cancellationToken = default);
    Task<Photo?> FindByPathAsync(string path, CancellationToken cancellationToken = default);
    Task<Photo?> FindFingerprintCandidateAsync(long size, string quickHash, string exceptPath, CancellationToken cancellationToken = default);
    Task UpsertBatchAsync(IReadOnlyCollection<PhotoDraft> photos, CancellationToken cancellationToken = default);
    Task SetFullHashAsync(long photoId, string fullHash, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetTagsAsync(long photoId, CancellationToken cancellationToken = default);
    Task AddTagsAsync(IReadOnlyCollection<long> photoIds, IReadOnlyCollection<string> tags, CancellationToken cancellationToken = default);
    Task RemoveTagAsync(IReadOnlyCollection<long> photoIds, string tag, CancellationToken cancellationToken = default);
    Task SetRatingAsync(IReadOnlyCollection<long> photoIds, int rating, CancellationToken cancellationToken = default);
    Task SetNotesAsync(long photoId, string notes, CancellationToken cancellationToken = default);
    Task<int> MarkMissingAsync(CancellationToken cancellationToken = default);
    Task BackupAsync(string destination, CancellationToken cancellationToken = default);
}
