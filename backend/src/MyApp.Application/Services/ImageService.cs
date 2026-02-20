using System.Collections.Concurrent;
using MyApp.Application.Models;
using MyApp.Application.Repositories;

namespace MyApp.Application.Services;

public sealed class ImageService
{
    private readonly IImageRepository _repo;
    private readonly string _uploadsPath;

    private readonly ConcurrentDictionary<Guid, ConcurrentDictionary<string, byte>> _votes = new();

    public ImageService(IImageRepository repo, string uploadsPath)
    {
        _repo = repo;
        _uploadsPath = uploadsPath;
        Directory.CreateDirectory(_uploadsPath);
    }

    public async Task<ImageRecord> UploadAsync(
        string title,
        string contentType,
        long sizeBytes,
        Stream fileStream,
        string originalFileName,
        string userId,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Название обязательно", nameof(title));

        if (sizeBytes <= 0)
            throw new ArgumentException("Файл пустой", nameof(sizeBytes));

        var safeName = MakeSafeFileName(originalFileName);
        var storedName = $"{Guid.NewGuid():N}_{safeName}";
        var fullPath = Path.Combine(_uploadsPath, storedName);

        await using (var outStream = File.Create(fullPath))
        {
            await fileStream.CopyToAsync(outStream, ct);
        }

        var rec = new ImageRecord
        {
            Title = title.Trim(),
            FileName = storedName,
            ContentType = contentType,
            SizeBytes = sizeBytes,
            UploadedByUserId = userId
        };

        var created = await _repo.AddAsync(rec, ct);
        _votes.TryAdd(created.Id, new ConcurrentDictionary<string, byte>());

        return created;
    }

    public Task<ImageRecord?> GetAsync(Guid id, CancellationToken ct) => _repo.GetAsync(id, ct);

    public Task<IReadOnlyList<ImageRecord>> ListAsync(CancellationToken ct) => _repo.ListAsync(ct);

    public string GetFilePath(ImageRecord rec) => Path.Combine(_uploadsPath, rec.FileName);

    public void Vote(Guid imageId, string userName, bool isLike)
    {
        if (imageId == Guid.Empty) throw new ArgumentException("imageId", nameof(imageId));
        if (string.IsNullOrWhiteSpace(userName)) throw new ArgumentException("userName", nameof(userName));

        var img = _repo.GetAsync(imageId, CancellationToken.None).GetAwaiter().GetResult();
        if (img is null) throw new KeyNotFoundException();

        if (isLike)
            VoteAsync(imageId, userName, CancellationToken.None).GetAwaiter().GetResult();
        else
            UnvoteAsync(imageId, userName, CancellationToken.None).GetAwaiter().GetResult();
    }

    public void Unvote(Guid imageId, string userName)
    {
        if (imageId == Guid.Empty) throw new ArgumentException("imageId", nameof(imageId));
        if (string.IsNullOrWhiteSpace(userName)) throw new ArgumentException("userName", nameof(userName));

        var img = _repo.GetAsync(imageId, CancellationToken.None).GetAwaiter().GetResult();
        if (img is null) throw new KeyNotFoundException();

        UnvoteAsync(imageId, userName, CancellationToken.None).GetAwaiter().GetResult();
    }

    public IReadOnlyList<ImageRatingItem> GetRatingTop(int top, string baseUrl)
    {
        if (top <= 0) top = 50;
        return GetRatingTopAsync(top, CancellationToken.None).GetAwaiter().GetResult();
    }

    public Task VoteAsync(Guid imageId, string userId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("userId", nameof(userId));

        var set = _votes.GetOrAdd(imageId, _ => new ConcurrentDictionary<string, byte>());
        set[userId] = 1;
        return Task.CompletedTask;
    }

    public Task UnvoteAsync(Guid imageId, string userId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("userId", nameof(userId));

        if (_votes.TryGetValue(imageId, out var set))
            set.TryRemove(userId, out _);

        return Task.CompletedTask;
    }

    public async Task<IReadOnlyList<ImageRatingItem>> GetRatingTopAsync(int take, CancellationToken ct)
    {
        if (take <= 0) take = 10;

        var images = await _repo.ListAsync(ct);

        var items = images.Select(img =>
        {
            var votes = _votes.TryGetValue(img.Id, out var set) ? set.Count : 0;
            return new ImageRatingItem
            {
                ImageId = img.Id,
                Title = img.Title,
                Votes = votes
            };
        });

        return items
            .OrderByDescending(x => x.Votes)
            .ThenBy(x => x.Title)
            .Take(take)
            .ToList();
    }

    private static string MakeSafeFileName(string name)
    {
        var bad = Path.GetInvalidFileNameChars();
        var cleaned = new string(name.Select(ch => bad.Contains(ch) ? '_' : ch).ToArray());
        return string.IsNullOrWhiteSpace(cleaned) ? "file" : cleaned;
    }
}
