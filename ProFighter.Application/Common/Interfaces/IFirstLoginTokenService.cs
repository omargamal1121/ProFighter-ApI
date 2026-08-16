namespace ProFighter.Application.Common.Interfaces;

public interface IFirstLoginTokenService
{
    string GenerateToken(string mobileNumber);
    string? ValidateAndConsumeToken(string token);
}
