using Microsoft.Data.Sqlite;
using PhotoManager.Application.Abstractions;
using PhotoManager.Application.Models;
using PhotoManager.Domain.Entities;

namespace PhotoManager.Infrastructure.Data;

public sealed class SqlitePhotoCatalog(string databasePath) : IPhotoCatalog
{
    private readonly string _databasePath = Path.GetFullPath(databasePath);
    private string ConnectionString => new SqliteConnectionStringBuilder { DataSource = _databasePath, Mode = SqliteOpenMode.ReadWriteCreate, Cache = SqliteCacheMode.Shared }.ToString();

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_databasePath)!);
        await using var connection = await OpenAsync(cancellationToken);
        var command = connection.CreateCommand();
        command.CommandText = """
            PRAGMA journal_mode=WAL;
            PRAGMA synchronous=NORMAL;
            PRAGMA foreign_keys=ON;
            PRAGMA busy_timeout=5000;
            CREATE TABLE IF NOT EXISTS photos (
              id INTEGER PRIMARY KEY, path TEXT NOT NULL UNIQUE COLLATE NOCASE, file_name TEXT NOT NULL,
              file_size INTEGER NOT NULL, last_write_utc TEXT NOT NULL, quick_hash TEXT NOT NULL,
              full_hash TEXT, width INTEGER, height INTEGER, captured_at TEXT, camera TEXT, lens TEXT,
              rating INTEGER NOT NULL DEFAULT 0 CHECK(rating BETWEEN 0 AND 5), notes TEXT NOT NULL DEFAULT '',
              is_missing INTEGER NOT NULL DEFAULT 0, indexed_at_utc TEXT NOT NULL);
            CREATE INDEX IF NOT EXISTS ix_photos_fingerprint ON photos(file_size, quick_hash);
            CREATE INDEX IF NOT EXISTS ix_photos_captured ON photos(captured_at DESC);
            CREATE TABLE IF NOT EXISTS tags (id INTEGER PRIMARY KEY, name TEXT NOT NULL UNIQUE COLLATE NOCASE);
            CREATE TABLE IF NOT EXISTS photo_tags (
              photo_id INTEGER NOT NULL REFERENCES photos(id) ON DELETE CASCADE,
              tag_id INTEGER NOT NULL REFERENCES tags(id) ON DELETE CASCADE,
              PRIMARY KEY(photo_id, tag_id));
            PRAGMA user_version=2;
            """;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Photo>> QueryAsync(PhotoQuery query, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenAsync(cancellationToken);
        var command = connection.CreateCommand();
        var where = BuildWhere(command, query);
        command.CommandText = $"""
            SELECT DISTINCT p.* FROM photos p
            {(!string.IsNullOrWhiteSpace(query.Tag) ? "JOIN photo_tags pt ON pt.photo_id=p.id JOIN tags t ON t.id=pt.tag_id" : "")}
            {where}
            ORDER BY COALESCE(p.captured_at, p.indexed_at_utc) DESC, p.id DESC LIMIT $limit OFFSET $offset;
            """;
        command.Parameters.AddWithValue("$limit", Math.Clamp(query.Limit, 1, 500));
        command.Parameters.AddWithValue("$offset", Math.Max(0, query.Offset));
        var result = new List<Photo>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken)) result.Add(Map(reader));
        return result;
    }

    public async Task<int> CountAsync(PhotoQuery query, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenAsync(cancellationToken);
        var command = connection.CreateCommand();
        var where = BuildWhere(command, query);
        command.CommandText = $"SELECT COUNT(DISTINCT p.id) FROM photos p {(!string.IsNullOrWhiteSpace(query.Tag) ? "JOIN photo_tags pt ON pt.photo_id=p.id JOIN tags t ON t.id=pt.tag_id" : "")} {where};";
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
    }

    private static string BuildWhere(SqliteCommand command, PhotoQuery query)
    {
        var clauses = new List<string>();
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            clauses.Add("(p.file_name LIKE $search ESCAPE '\\' OR p.path LIKE $search ESCAPE '\\' OR p.notes LIKE $search ESCAPE '\\')");
            command.Parameters.AddWithValue("$search", $"%{EscapeLike(query.Search.Trim())}%");
        }
        if (!string.IsNullOrWhiteSpace(query.Tag))
        {
            clauses.Add("t.name LIKE $tag ESCAPE '\\'");
            command.Parameters.AddWithValue("$tag", $"%{EscapeLike(query.Tag.Trim())}%");
        }
        if (query.MinimumRating > 0) { clauses.Add("p.rating >= $rating"); command.Parameters.AddWithValue("$rating", query.MinimumRating); }
        return clauses.Count == 0 ? "" : "WHERE " + string.Join(" AND ", clauses);
    }

    private static string EscapeLike(string value) => value.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("%", "\\%", StringComparison.Ordinal).Replace("_", "\\_", StringComparison.Ordinal);

    public async Task<Photo?> FindByPathAsync(string path, CancellationToken cancellationToken = default) =>
        await FindOneAsync("SELECT * FROM photos WHERE path=$path COLLATE NOCASE LIMIT 1", ("$path", Path.GetFullPath(path)), cancellationToken);

    public async Task<Photo?> FindFingerprintCandidateAsync(long size, string quickHash, string exceptPath, CancellationToken cancellationToken = default) =>
        await FindOneAsync("SELECT * FROM photos WHERE file_size=$size AND quick_hash=$hash AND path<>$path COLLATE NOCASE AND is_missing=0 LIMIT 1",
            ("$size", size), ("$hash", quickHash), ("$path", Path.GetFullPath(exceptPath)), cancellationToken);

    private async Task<Photo?> FindOneAsync(string sql, (string, object) parameter, CancellationToken ct) => await FindOneAsync(sql, [parameter], ct);
    private async Task<Photo?> FindOneAsync(string sql, (string, object) a, (string, object) b, (string, object) c, CancellationToken ct) => await FindOneAsync(sql, [a, b, c], ct);
    private async Task<Photo?> FindOneAsync(string sql, IReadOnlyList<(string, object)> parameters, CancellationToken ct)
    {
        await using var connection = await OpenAsync(ct);
        var command = connection.CreateCommand(); command.CommandText = sql;
        foreach (var (name, value) in parameters) command.Parameters.AddWithValue(name, value);
        await using var reader = await command.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? Map(reader) : null;
    }

    public async Task UpsertBatchAsync(IReadOnlyCollection<PhotoDraft> photos, CancellationToken cancellationToken = default)
    {
        if (photos.Count == 0) return;
        await using var connection = await OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        foreach (var photo in photos)
        {
            var command = connection.CreateCommand(); command.Transaction = (SqliteTransaction)transaction;
            command.CommandText = """
                INSERT INTO photos(path,file_name,file_size,last_write_utc,quick_hash,full_hash,width,height,captured_at,camera,lens,indexed_at_utc,is_missing)
                VALUES($path,$name,$size,$write,$quick,$full,$width,$height,$captured,$camera,$lens,$indexed,0)
                ON CONFLICT(path) DO UPDATE SET file_name=excluded.file_name,file_size=excluded.file_size,last_write_utc=excluded.last_write_utc,
                quick_hash=excluded.quick_hash,full_hash=COALESCE(excluded.full_hash,photos.full_hash),width=excluded.width,height=excluded.height,
                captured_at=excluded.captured_at,camera=excluded.camera,lens=excluded.lens,indexed_at_utc=excluded.indexed_at_utc,is_missing=0;
                """;
            Add(command, "$path", photo.Path); Add(command, "$name", photo.FileName); Add(command, "$size", photo.FileSize);
            Add(command, "$write", photo.LastWriteUtc.ToString("O")); Add(command, "$quick", photo.QuickHash); Add(command, "$full", photo.FullHash);
            Add(command, "$width", photo.Width); Add(command, "$height", photo.Height); Add(command, "$captured", photo.CapturedAt?.ToString("O"));
            Add(command, "$camera", photo.Camera); Add(command, "$lens", photo.Lens); Add(command, "$indexed", DateTime.UtcNow.ToString("O"));
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task SetFullHashAsync(long photoId, string fullHash, CancellationToken cancellationToken = default) =>
        await ExecuteAsync("UPDATE photos SET full_hash=$value WHERE id=$id", cancellationToken, ("$value", fullHash), ("$id", photoId));

    public async Task<IReadOnlyList<string>> GetTagsAsync(long photoId, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenAsync(cancellationToken); var command = connection.CreateCommand();
        command.CommandText = "SELECT t.name FROM tags t JOIN photo_tags pt ON pt.tag_id=t.id WHERE pt.photo_id=$id ORDER BY t.name"; command.Parameters.AddWithValue("$id", photoId);
        var result = new List<string>(); await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken)) result.Add(reader.GetString(0)); return result;
    }

    public async Task AddTagsAsync(IReadOnlyCollection<long> photoIds, IReadOnlyCollection<string> tags, CancellationToken cancellationToken = default)
    {
        var clean = tags.Select(x => x.Trim()).Where(x => x.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        if (photoIds.Count == 0 || clean.Length == 0) return;
        await using var connection = await OpenAsync(cancellationToken); await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        foreach (var tag in clean)
        {
            var insert = connection.CreateCommand(); insert.Transaction = (SqliteTransaction)transaction;
            insert.CommandText = "INSERT OR IGNORE INTO tags(name) VALUES($name);"; insert.Parameters.AddWithValue("$name", tag); await insert.ExecuteNonQueryAsync(cancellationToken);
            var idCommand = connection.CreateCommand(); idCommand.Transaction = (SqliteTransaction)transaction;
            idCommand.CommandText = "SELECT id FROM tags WHERE name=$name COLLATE NOCASE"; idCommand.Parameters.AddWithValue("$name", tag);
            var tagId = Convert.ToInt64(await idCommand.ExecuteScalarAsync(cancellationToken));
            foreach (var photoId in photoIds)
            {
                var bind = connection.CreateCommand(); bind.Transaction = (SqliteTransaction)transaction;
                bind.CommandText = "INSERT OR IGNORE INTO photo_tags(photo_id,tag_id) VALUES($photo,$tag)";
                bind.Parameters.AddWithValue("$photo", photoId); bind.Parameters.AddWithValue("$tag", tagId); await bind.ExecuteNonQueryAsync(cancellationToken);
            }
        }
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task RemoveTagAsync(IReadOnlyCollection<long> photoIds, string tag, CancellationToken cancellationToken = default)
    {
        foreach (var id in photoIds) await ExecuteAsync("DELETE FROM photo_tags WHERE photo_id=$id AND tag_id=(SELECT id FROM tags WHERE name=$tag COLLATE NOCASE)", cancellationToken, ("$id", id), ("$tag", tag));
        await ExecuteAsync("DELETE FROM tags WHERE id NOT IN (SELECT tag_id FROM photo_tags)", cancellationToken);
    }

    public async Task SetRatingAsync(IReadOnlyCollection<long> photoIds, int rating, CancellationToken cancellationToken = default)
    {
        rating = Math.Clamp(rating, 0, 5); foreach (var id in photoIds) await ExecuteAsync("UPDATE photos SET rating=$value WHERE id=$id", cancellationToken, ("$value", rating), ("$id", id));
    }

    public Task SetNotesAsync(long photoId, string notes, CancellationToken cancellationToken = default) =>
        ExecuteAsync("UPDATE photos SET notes=$value WHERE id=$id", cancellationToken, ("$value", notes), ("$id", photoId));

    public async Task<int> MarkMissingAsync(CancellationToken cancellationToken = default)
    {
        var rows = await QueryAsync(new(Limit: 500), cancellationToken); var offset = rows.Count; var missing = 0;
        while (rows.Count > 0)
        {
            foreach (var photo in rows) { var value = !File.Exists(photo.Path); if (value) missing++; await ExecuteAsync("UPDATE photos SET is_missing=$value WHERE id=$id", cancellationToken, ("$value", value ? 1 : 0), ("$id", photo.Id)); }
            rows = await QueryAsync(new(Offset: offset, Limit: 500), cancellationToken); offset += rows.Count;
        }
        return missing;
    }

    public async Task BackupAsync(string destination, CancellationToken cancellationToken = default)
    {
        await using var source = await OpenAsync(cancellationToken); await using var target = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = destination }.ToString());
        await target.OpenAsync(cancellationToken); source.BackupDatabase(target);
    }

    private async Task ExecuteAsync(string sql, CancellationToken ct, params (string Name, object? Value)[] parameters)
    {
        await using var connection = await OpenAsync(ct); var command = connection.CreateCommand(); command.CommandText = sql;
        foreach (var (name, value) in parameters) Add(command, name, value); await command.ExecuteNonQueryAsync(ct);
    }

    private async Task<SqliteConnection> OpenAsync(CancellationToken cancellationToken)
    {
        var connection = new SqliteConnection(ConnectionString); await connection.OpenAsync(cancellationToken);
        var command = connection.CreateCommand(); command.CommandText = "PRAGMA foreign_keys=ON; PRAGMA busy_timeout=5000;"; await command.ExecuteNonQueryAsync(cancellationToken); return connection;
    }

    private static void Add(SqliteCommand command, string name, object? value) => command.Parameters.AddWithValue(name, value ?? DBNull.Value);
    private static Photo Map(SqliteDataReader r) => new(r.GetInt64(r.GetOrdinal("id")), r.GetString(r.GetOrdinal("path")), r.GetString(r.GetOrdinal("file_name")),
        r.GetInt64(r.GetOrdinal("file_size")), DateTime.Parse(r.GetString(r.GetOrdinal("last_write_utc")), null, System.Globalization.DateTimeStyles.RoundtripKind),
        r.GetString(r.GetOrdinal("quick_hash")), NullableString(r, "full_hash"), NullableInt(r, "width"), NullableInt(r, "height"), NullableDate(r, "captured_at"),
        NullableString(r, "camera"), NullableString(r, "lens"), r.GetInt32(r.GetOrdinal("rating")), r.GetString(r.GetOrdinal("notes")), r.GetInt32(r.GetOrdinal("is_missing")) != 0,
        DateTime.Parse(r.GetString(r.GetOrdinal("indexed_at_utc")), null, System.Globalization.DateTimeStyles.RoundtripKind));
    private static string? NullableString(SqliteDataReader r, string name) { var i = r.GetOrdinal(name); return r.IsDBNull(i) ? null : r.GetString(i); }
    private static int? NullableInt(SqliteDataReader r, string name) { var i = r.GetOrdinal(name); return r.IsDBNull(i) ? null : r.GetInt32(i); }
    private static DateTime? NullableDate(SqliteDataReader r, string name) { var value = NullableString(r, name); return value is null ? null : DateTime.Parse(value, null, System.Globalization.DateTimeStyles.RoundtripKind); }
}
