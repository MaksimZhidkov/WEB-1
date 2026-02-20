namespace MyApp.Infrastructure.Data;

public sealed class ImageRow
{
    public Guid Id { get; set; }
    public string Title { get; set; } = "";
    public string RelativePath { get; set; } = "";
    public DateTime CreatedAtUtc { get; set; }
}
