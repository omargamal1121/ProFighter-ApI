using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ProFighter.Application.Common.Interfaces;
using System.Security.Cryptography;
using System.Text;

namespace ProFighter.Infrastructure.Caching;

public class FirstLoginTokenService : IFirstLoginTokenService
{
    private readonly IMemoryCache _cache;
    private readonly IConfiguration _configuration;
    private readonly ILogger<FirstLoginTokenService> _logger;
    private readonly int _expiryMinutes;

    public FirstLoginTokenService(
        IMemoryCache cache,
        IConfiguration configuration,
        ILogger<FirstLoginTokenService> logger)
    {
        _cache = cache;
        _configuration = configuration;
        _logger = logger;
        _expiryMinutes = int.TryParse(configuration["Auth:FirstLoginTokenExpiryMinutes"], out var minutes)
            ? minutes
            : 15;
    }

    public string GenerateToken(string mobileNumber)
    {
        var tokenBytes = RandomNumberGenerator.GetBytes(32);
        var token = Convert.ToBase64String(tokenBytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .Replace("=", "");

        var cacheKey = $"first-login-token:{token}";
        var options = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_expiryMinutes)
        };

        _cache.Set(cacheKey, mobileNumber, options);
        _logger.LogInformation("Generated first-login token for mobile number {MobileNumber}, expires in {Minutes} minutes",
            mobileNumber, _expiryMinutes);

        return token;
    }

    public string? ValidateAndConsumeToken(string token)
    {
        var cacheKey = $"first-login-token:{token}";

        if (_cache.TryGetValue(cacheKey, out string? mobileNumber) && mobileNumber != null)
        {
            _cache.Remove(cacheKey);
            _logger.LogInformation("First-login token validated and consumed for mobile number {MobileNumber}", mobileNumber);
            return mobileNumber;
        }

        _logger.LogWarning("Invalid or expired first-login token: {Token}", token);
        return null;
    }
}
