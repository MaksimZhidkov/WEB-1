using MyApp.Domain.Entities;

namespace MyApp.Application.Interfaces;

public interface IImageRepository
{
    int Count { get; }

    ImageItem Add(ImageItem item);
    ImageItem? Get(Guid id);
    bool Delete(Guid id);

    IReadOnlyList<ImageItem> GetPage(int page, int pageSize, out int totalCount);
    IReadOnlyList<ImageItem> GetRatingTop(int top);

    void SetVote(Guid imageId, string userName, bool isLike);
    void RemoveVote(Guid imageId, string userName);
}
