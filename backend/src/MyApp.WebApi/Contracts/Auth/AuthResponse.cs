namespace MyApp.WebApi.Contracts.Auth;

public sealed class AuthResponse
{
    public required string AccessToken { get; init; }
    public string TokenType { get; init; } = "Bearer";
}
