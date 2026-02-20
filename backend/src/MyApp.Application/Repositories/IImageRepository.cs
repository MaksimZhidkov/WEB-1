using MyApp.Application.Models;

namespace MyApp.Application.Repositories;

public interface IImageRepository
{
    Task<ImageRecord> AddAsync(ImageRecord img, CancellationToken ct);
    Task<ImageRecord?> GetAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<ImageRecord>> ListAsync(CancellationToken ct);
}
