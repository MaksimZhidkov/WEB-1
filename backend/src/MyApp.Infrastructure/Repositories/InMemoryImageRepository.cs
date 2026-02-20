using System.Collections.Concurrent;
using MyApp.Application.Models;
using MyApp.Application.Repositories;

namespace MyApp.Infrastructure.Repositories;

public sealed class InMemoryImageRepository : IImageRepository
{
    private readonly ConcurrentDictionary<Guid, ImageRecord> _db = new();

    public Task<ImageRecord> AddAsync(ImageRecord img, CancellationToken ct)
    {
        _db[img.Id] = img;
        return Task.FromResult(img);
    }

    public Task<ImageRecord?> GetAsync(Guid id, CancellationToken ct)
    {
        _db.TryGetValue(id, out var img);
        return Task.FromResult(img);
    }

    public Task<IReadOnlyList<ImageRecord>> ListAsync(CancellationToken ct)
    {
        IReadOnlyList<ImageRecord> list = _db.Values
            .OrderByDescending(x => x.UploadedAt)
            .ToList();
        return Task.FromResult(list);
    }
}
