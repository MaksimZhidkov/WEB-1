using Microsoft.EntityFrameworkCore;
using MyApp.Application.Interfaces;
using MyApp.Domain.Entities;
using MyApp.Infrastructure.Data;

namespace MyApp.Infrastructure.Repositories;

public sealed class DbImageRepository : IImageRepository
{
    private const int MaxItems = 50;
    private readonly AppDbContext _db;

    public DbImageRepository(AppDbContext db) => _db = db;

    public int Count => _db.Images.Count();

    public ImageItem Add(ImageItem item)
    {
        // лимит 50 записей
        if (_db.Images.Count() >= MaxItems)
            throw new InvalidOperationException($"Лимит хранилища: {MaxItems} записей.");

        var row = new ImageRow
        {
            Id = item.Id,
            Title = item.Title,
            RelativePath = item.RelativePath,
            CreatedAtUtc = item.CreatedAtUtc
        };

        _db.Images.Add(row);
        _db.SaveChanges();

        return item;
    }

    public ImageItem? Get(Guid id)
    {
        var img = _db.Images.AsNoTracking().FirstOrDefault(x => x.Id == id);
        if (img is null) return null;

        // IQueryable + LINQ: достаём голоса по imageId
        var votes = _db.Votes.AsNoTracking()
            .Where(v => v.ImageId == id)
            .ToList();

        var item = new ImageItem
        {
            Id = img.Id,
            Title = img.Title,
            RelativePath = img.RelativePath,
            CreatedAtUtc = img.CreatedAtUtc
        };

        foreach (var v in votes)
            item.VotesByUser[v.UserName] = v.IsLike;

        return item;
    }

    public bool Delete(Guid id)
    {
        var row = _db.Images.FirstOrDefault(x => x.Id == id);
        if (row is null) return false;

        _db.Images.Remove(row);
        _db.SaveChanges();
        return true;
    }

    public IReadOnlyList<ImageItem> GetPage(int page, int pageSize, out int totalCount)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);

        IQueryable<ImageRow> q = _db.Images.AsNoTracking();

        totalCount = q.Count();

        var rows = q.OrderByDescending(x => x.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        // Берём голоса пачкой по всем id на странице
        var ids = rows.Select(r => r.Id).ToList();

        var votes = _db.Votes.AsNoTracking()
            .Where(v => ids.Contains(v.ImageId))
            .ToList()
            .GroupBy(v => v.ImageId)
            .ToDictionary(g => g.Key, g => g.ToList());

        return rows.Select(r =>
        {
            var item = new ImageItem
            {
                Id = r.Id,
                Title = r.Title,
                RelativePath = r.RelativePath,
                CreatedAtUtc = r.CreatedAtUtc
            };

            if (votes.TryGetValue(r.Id, out var list))
                foreach (var v in list)
                    item.VotesByUser[v.UserName] = v.IsLike;

            return item;
        }).ToList();
    }

    public IReadOnlyList<ImageItem> GetRatingTop(int top)
    {
        top = Math.Clamp(top, 1, 50);

        // Берём все изображения и голоса (для ЛР2 допустимо; дальше можно оптимизировать)
        var imgs = _db.Images.AsNoTracking().ToList();
        var votes = _db.Votes.AsNoTracking().ToList()
            .GroupBy(v => v.ImageId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var items = imgs.Select(img =>
        {
            var item = new ImageItem
            {
                Id = img.Id,
                Title = img.Title,
                RelativePath = img.RelativePath,
                CreatedAtUtc = img.CreatedAtUtc
            };

            if (votes.TryGetValue(img.Id, out var list))
                foreach (var v in list)
                    item.VotesByUser[v.UserName] = v.IsLike;

            return item;
        });

        return items
            .OrderByDescending(x => x.Score)
            .ThenByDescending(x => x.LikesCount)
            .ThenByDescending(x => x.CreatedAtUtc)
            .Take(top)
            .ToList();
    }

    public void SetVote(Guid imageId, string userName, bool isLike)
    {
        userName = (userName ?? "").Trim();
        if (string.IsNullOrWhiteSpace(userName)) throw new ArgumentException("userName обязателен.");

        // проверяем, что картинка существует
        var exists = _db.Images.Any(x => x.Id == imageId);
        if (!exists) throw new KeyNotFoundException("Картинка не найдена.");

        var row = _db.Votes.FirstOrDefault(v => v.ImageId == imageId && v.UserName == userName);
        if (row is null)
        {
            _db.Votes.Add(new VoteRow { ImageId = imageId, UserName = userName, IsLike = isLike });
        }
        else
        {
            row.IsLike = isLike;
            _db.Votes.Update(row);
        }

        _db.SaveChanges();
    }

    public void RemoveVote(Guid imageId, string userName)
    {
        userName = (userName ?? "").Trim();
        if (string.IsNullOrWhiteSpace(userName)) throw new ArgumentException("userName обязателен.");

        var exists = _db.Images.Any(x => x.Id == imageId);
        if (!exists) throw new KeyNotFoundException("Картинка не найдена.");

        var row = _db.Votes.FirstOrDefault(v => v.ImageId == imageId && v.UserName == userName);
        if (row is null) return;

        _db.Votes.Remove(row);
        _db.SaveChanges();
    }
}
