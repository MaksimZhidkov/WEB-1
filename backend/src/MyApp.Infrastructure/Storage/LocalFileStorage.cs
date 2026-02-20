using MyApp.Application.Interfaces;

namespace MyApp.Infrastructure.Storage;

public sealed class LocalFileStorage : IFileStorage
{
    private readonly string _uploadsPhysicalPath; // .../wwwroot/uploads
    private readonly long _maxBytes;
    private readonly HashSet<string> _allowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/webp"
    };

    public LocalFileStorage(string uploadsPhysicalPath, long maxBytes = 5 * 1024 * 1024)
    {
        _uploadsPhysicalPath = uploadsPhysicalPath;
        _maxBytes = maxBytes;
        Directory.CreateDirectory(_uploadsPhysicalPath);
    }

    public async Task<string> SaveAsync(Stream content, string originalFileName, string contentType, CancellationToken ct)
    {
        if (!_allowedContentTypes.Contains(contentType))
            throw new InvalidOperationException($"Недопустимый тип файла: {contentType}");

        // ограничение по размеру: читаем в файл с подсчётом
        var ext = Path.GetExtension(originalFileName); // разрешение
        if (string.IsNullOrWhiteSpace(ext)) ext = ContentTypeToExt(contentType); // разрешение из mime

        var fileName = $"{Guid.NewGuid():N}{ext.ToLowerInvariant()}"; // ид + расширение
        var physical = Path.Combine(_uploadsPhysicalPath, fileName);

        long total = 0;
        // от перезаписи, асинхронное освоюождение ресурса
        await using var fs = new FileStream(physical, FileMode.CreateNew, FileAccess.Write, FileShare.None);

        var buffer = new byte[64 * 1024];
        int read;
        while ((read = await content.ReadAsync(buffer.AsMemory(0, buffer.Length), ct)) > 0)
        {
            total += read;
            if (total > _maxBytes)
            {
                fs.Close();
                File.Delete(physical);
                throw new InvalidOperationException($"Файл слишком большой. Лимит: {_maxBytes / (1024 * 1024)} MB");
            }
            await fs.WriteAsync(buffer.AsMemory(0, read), ct);
        }

        return $"/uploads/{fileName}";
    }

    public bool Delete(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath)) return false;

        var fileName = relativePath.Replace("\\", "/").Split('/').LastOrDefault();
        if (string.IsNullOrWhiteSpace(fileName)) return false;

        var physical = Path.Combine(_uploadsPhysicalPath, fileName);
        if (!File.Exists(physical)) return false; // проверка есть ли файл на диске

        File.Delete(physical);
        return true;
    }

    private static string ContentTypeToExt(string contentType) => contentType switch
    {
        "image/jpeg" => ".jpg",
        "image/png" => ".png",
        "image/webp" => ".webp",
        _ => ".bin"
    };
}
