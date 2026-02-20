namespace MyApp.Application.Models;

public sealed class ImageRatingItem
{
    public required Guid ImageId { get; init; }
    public required string Title { get; init; }
    public int Votes { get; init; }
}
