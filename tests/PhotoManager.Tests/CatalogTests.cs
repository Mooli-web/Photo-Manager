using Xunit;
using PhotoManager.Application.Models;
using PhotoManager.Domain.Entities;
using PhotoManager.Infrastructure.Data;

namespace PhotoManager.Tests;

public sealed class CatalogTests : IAsyncLifetime
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "PhotoManagerTests", Guid.NewGuid().ToString("N"));
    private SqlitePhotoCatalog _catalog = null!;

    public async ValueTask InitializeAsync()
    {
        Directory.CreateDirectory(_folder); _catalog = new(Path.Combine(_folder, "test.sqlite3")); await _catalog.InitializeAsync();
    }

    public ValueTask DisposeAsync() { try { Directory.Delete(_folder, true); } catch (IOException) { } return ValueTask.CompletedTask; }

    [Fact]
    public async Task Upsert_query_and_tags_are_consistent()
    {
        var path = Path.Combine(_folder, "photo.jpg");
        var draft = new PhotoDraft(path, "photo.jpg", 42, DateTime.UtcNow, "quick", null, 100, 50, null, "Camera", null);
        await _catalog.UpsertBatchAsync([draft]);
        var photos = await _catalog.QueryAsync(new(Search: "photo"));
        Assert.Single(photos);
        await _catalog.AddTagsAsync([photos[0].Id], ["Nature", "Nature", "طبیعت"]);
        Assert.Equal(["Nature", "طبیعت"], await _catalog.GetTagsAsync(photos[0].Id));
        Assert.Equal(1, await _catalog.CountAsync(new(Tag: "nature")));
    }

    [Fact]
    public async Task Same_filename_in_different_paths_is_not_a_collision()
    {
        var now = DateTime.UtcNow;
        await _catalog.UpsertBatchAsync([
            new(Path.Combine(_folder, "a", "same.jpg"), "same.jpg", 1, now, "one", null, null, null, null, null, null),
            new(Path.Combine(_folder, "b", "same.jpg"), "same.jpg", 2, now, "two", null, null, null, null, null, null)]);
        Assert.Equal(2, await _catalog.CountAsync(new()));
    }
}
