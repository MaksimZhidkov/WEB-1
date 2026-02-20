using System.Collections.Concurrent;
using MyApp.Application.Interfaces;
using MyApp.Domain.Entities;

namespace MyApp.Infrastructure.Repositories;

public sealed class InMemoryImageRepository : IImageRepository
{
    private const int MaxItems = 50;
    private readonly ConcurrentDictionary<Guid, ImageItem> _items = new();

    public int Count => _items.Count;

    public ImageItem Add(ImageItem item)
    {
        if (_items.Count >= MaxItems)
            throw new InvalidOperationException($"Лимит хранилища: {MaxItems} записей.");

        if (!_items.TryAdd(item.Id, item))
            throw new InvalidOperationException("Не удалось добавить запись.");

        return item;
    }

    public ImageItem? Get(Guid id) => _items.TryGetValue(id, out var x) ? x : null;

    public bool Delete(Guid id) => _items.TryRemove(id, out _);

    public IReadOnlyList<ImageItem> GetPage(int page, int pageSize, out int totalCount)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);

        var ordered = _items.Values
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToList();

        totalCount = ordered.Count;

        return ordered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();
    }

    public IReadOnlyList<ImageItem> GetRatingTop(int top)
    {
        top = Math.Clamp(top, 1, 50);

        return _items.Values
            .OrderByDescending(x => x.Score)
            .ThenByDescending(x => x.LikesCount)
            .ThenByDescending(x => x.CreatedAtUtc)
            .Take(top)
            .ToList();
    }

    public void SetVote(Guid imageId, string userName, bool isLike)
    {
        var item = Get(imageId) ?? throw new KeyNotFoundException("Картинка не найдена.");
        userName = (userName ?? "").Trim();
        if (string.IsNullOrWhiteSpace(userName)) throw new ArgumentException("userName обязателен.");

        lock (item.VotesByUser)
        {
            item.VotesByUser[userName] = isLike;
        }
    }

    public void RemoveVote(Guid imageId, string userName)
    {
        var item = Get(imageId) ?? throw new KeyNotFoundException("Картинка не найдена.");
        userName = (userName ?? "").Trim();
        if (string.IsNullOrWhiteSpace(userName)) throw new ArgumentException("userName обязателен.");

        lock (item.VotesByUser)
        {
            item.VotesByUser.Remove(userName);
        }
    }
}
