using MyApp.Application.DTO;
using MyApp.Application.Interfaces;
using MyApp.Domain.Entities;

namespace MyApp.Application.Services;

public sealed class ImageService
{
    private readonly IImageRepository _repo;
    private readonly IFileStorage _files;

    public ImageService(IImageRepository repo, IFileStorage files)
    {
        _repo = repo;
        _files = files;
    }

    public async Task<ImageDto> UploadAsync(
        string title,
        Stream content,
        string originalFileName,
        string contentType,
        string baseUrl,
        CancellationToken ct)
    {
        var relative = await _files.SaveAsync(content, originalFileName, contentType, ct);

        var item = new ImageItem
        {
            Title = title.Trim(),
            RelativePath = relative
        };

        item = _repo.Add(item);

        return ToDto(item, baseUrl);
    }

    public PagedResult<ImageDto> GetPage(int page, int pageSize, string baseUrl)
    {
        var items = _repo.GetPage(page, pageSize, out var total);
        return new PagedResult<ImageDto>
        {
            Items = items.Select(x => ToDto(x, baseUrl)).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        };
    }

    public ImageDto? Get(Guid id, string baseUrl)
    {
        var item = _repo.Get(id);
        return item is null ? null : ToDto(item, baseUrl);
    }

    public bool Delete(Guid id)
    {
        var item = _repo.Get(id);
        if (item is null) return false;

        var ok = _repo.Delete(id);
        if (ok)
        {
            _files.Delete(item.RelativePath);
        }
        return ok;
    }

    public void Vote(Guid id, string userName, bool isLike) => _repo.SetVote(id, userName, isLike);
    public void Unvote(Guid id, string userName) => _repo.RemoveVote(id, userName);

    public IReadOnlyList<RatingItemDto> GetRatingTop(int top, string baseUrl)
        => _repo.GetRatingTop(top).Select(x => new RatingItemDto
        {
            Id = x.Id,
            Title = x.Title,
            Url = BuildUrl(baseUrl, x.RelativePath),
            Likes = x.LikesCount,
            Dislikes = x.DislikesCount,
            Score = x.Score
        }).ToList();

    private static ImageDto ToDto(ImageItem x, string baseUrl) => new()
    {
        Id = x.Id,
        Title = x.Title,
        Url = BuildUrl(baseUrl, x.RelativePath),
        CreatedAtUtc = x.CreatedAtUtc,
        Likes = x.LikesCount,
        Dislikes = x.DislikesCount,
        Score = x.Score
    };

    private static string BuildUrl(string baseUrl, string relativePath)
    {
        if (relativePath.StartsWith("/")) relativePath = relativePath[1..];
        return $"{baseUrl.TrimEnd('/')}/{relativePath}";
    }
}
