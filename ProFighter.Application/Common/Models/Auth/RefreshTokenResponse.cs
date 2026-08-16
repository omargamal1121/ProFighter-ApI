namespace ProFighter.Application.Common.Models.Auth;

public class RefreshTokenResponse
{
    public string Token { get; set; } = null!;
    public string RefreshToken { get; set; } = null!;
}
