namespace MyApp.Domain.Entities;

public sealed class ImageItem
{
    // создаем уникальные, неизменяемые id
    public Guid Id { get; init; } = Guid.NewGuid(); //автоматически
    // названия картинок - базово пустая строка
    public string Title { get; set; } = string.Empty;
    // хранение пути к файлу на сервере
    public string RelativePath { get; set; } = string.Empty;
    // фиксация времени создания картинки
    public DateTime CreatedAtUtc { get; init; } = DateTime.UtcNow;
    //учет головосов по имени пользователся и лайку/дизлайку, Регистр у имен не играет роли
    public Dictionary<string, bool> VotesByUser { get; } = new(StringComparer.OrdinalIgnoreCase);

    // подсчет голосов на картинке
    // кв - контейнер, кв.кей - имя, кв.валуе - голос
    public int LikesCount => VotesByUser.Count(kv => kv.Value);
    public int DislikesCount => VotesByUser.Count(kv => !kv.Value);
    public int Score => LikesCount - DislikesCount;
}
