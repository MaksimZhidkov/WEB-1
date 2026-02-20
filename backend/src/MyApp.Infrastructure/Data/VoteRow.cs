namespace MyApp.Infrastructure.Data;

public sealed class VoteRow
{
    public Guid ImageId { get; set; }
    public string UserName { get; set; } = "";
    public bool IsLike { get; set; }

    public ImageRow? Image { get; set; }
}
