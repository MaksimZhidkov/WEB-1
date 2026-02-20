namespace MyApp.Application.Interfaces;

public interface IFileStorage
{
    Task<string> SaveAsync(Stream content, string originalFileName, string contentType, CancellationToken ct);
    bool Delete(string relativePath);
}
