using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using ProFighter.Application.Common.Interfaces.Auth;
using ProFighter.Application.Common.Models.Auth;

namespace ProFighter.Infrastructure.Auth.Services;

public sealed class TokenService : ITokenService
{
    private readonly ILogger<TokenService> _logger;
    private readonly string _secretKey;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly double _expiresInMinutes;

    public TokenService(
        ILogger<TokenService> logger,
        IConfiguration config)
    {
        _logger = logger;

        _secretKey = config["Jwt:Key"]
            ?? throw new InvalidOperationException("Jwt:Key is missing from configuration.");

        _issuer = config["Jwt:Issuer"]
            ?? throw new InvalidOperationException("Jwt:Issuer is missing from configuration.");

        _audience = config["Jwt:Audience"]
            ?? throw new InvalidOperationException("Jwt:Audience is missing from configuration.");

        if (_secretKey.Length < 32)
            throw new InvalidOperationException("JWT secret key must be at least 32 characters long.");

        _expiresInMinutes = double.TryParse(config["Jwt:ExpiresInMinutes"], out var minutes)
            ? minutes
            : 15;
    }

    public Task<string> GenerateTokenAsync(TokenGenerationRequest request, bool isAccountCompletion = false)
    {
        var tokenString = BuildToken(request, isAccountCompletion);
        return Task.FromResult(tokenString);
    }

    private string BuildToken(TokenGenerationRequest request, bool isAccountCompletion)
    {
        _logger.LogInformation("Generating access token for UserId: {UserId}", request.UserId);

        var jti = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Jti, jti),
            new(ClaimTypes.NameIdentifier, request.UserId.ToString()),
            new(JwtRegisteredClaimNames.Iat,
                DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
                ClaimValueTypes.Integer64)
        };

        if (isAccountCompletion)
        {
            claims.Add(new Claim("token_type", "account_completion"));
        }

        claims.AddRange(request.Roles.Select(role => new Claim(ClaimTypes.Role, role)));
        if (request.Claims != null)
            claims.AddRange(request.Claims);

        var key                = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
        var signingCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var notBefore          = DateTime.UtcNow.AddSeconds(-30); // small clock-skew tolerance
        
        // If it's an account completion token, use a short 15 minute expiry.
        // Otherwise use the configured expiry.
        var expiryMinutes = isAccountCompletion ? 15 : _expiresInMinutes;
        var expires            = DateTime.UtcNow.AddMinutes(expiryMinutes);

        var token = new JwtSecurityToken(
            issuer:             _issuer,
            audience:           _audience,
            notBefore:          notBefore,
            expires:            expires,
            claims:             claims,
            signingCredentials: signingCredentials);

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        _logger.LogInformation("Access token generated for UserId: {UserId}, expires at {ExpiresAt:u}",
            request.UserId, expires);

        return tokenString;
    }
}
