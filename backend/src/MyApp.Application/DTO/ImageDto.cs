namespace MyApp.Application.DTO;

// отрисовка карточек для пользователя с 
public sealed class ImageDto
{
    public required Guid Id { get; init; }
    public required string Title { get; init; }
    public required string Url { get; init; } // c url
    public required DateTime CreatedAtUtc { get; init; }
    public required int Likes { get; init; } // без контейнера имя = голос
    public required int Dislikes { get; init; }
    public required int Score { get; init; }
}
