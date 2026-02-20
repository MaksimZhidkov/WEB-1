namespace MyApp.Application.DTO;

public sealed class RatingItemDto
{
    public required Guid Id { get; init; }
    public required string Title { get; init; }
    public required string Url { get; init; }
    public required int Likes { get; init; }
    public required int Dislikes { get; init; }
    public required int Score { get; init; }
}
