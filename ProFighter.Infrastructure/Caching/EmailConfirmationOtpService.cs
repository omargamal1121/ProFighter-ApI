using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using ProFighter.Application.Common.Interfaces;

namespace ProFighter.Infrastructure.Caching;

public class EmailConfirmationOtpService : IEmailConfirmationOtpService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<EmailConfirmationOtpService> _logger;

    public EmailConfirmationOtpService(
        IMemoryCache cache,
        ILogger<EmailConfirmationOtpService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public void StoreOtp(string userId, string otp, TimeSpan expiry)
    {
        var cacheKey = $"email-confirmation-otp:{userId}";
        var options = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiry
        };

        _cache.Set(cacheKey, otp, options);
        
        // Verify storage by trying to retrieve immediately
        var testRetrieve = _cache.Get<string>(cacheKey);
        _logger.LogInformation("Stored email confirmation OTP for user {UserId}, OTP: {Otp}, Key: {Key}, Expiry: {Minutes}min, Retrieved: {Retrieved}",
            userId, otp, cacheKey, expiry.TotalMinutes, testRetrieve ?? "NULL");
    }

    public bool ValidateAndConsumeOtp(string userId, string otp)
    {
        var cacheKey = $"email-confirmation-otp:{userId}";
        
        // Log attempt details
        var existsBefore = _cache.TryGetValue(cacheKey, out string? storedOtp);
        _logger.LogInformation("Email OTP Validation attempt - UserId: {UserId}, Key: {Key}, Exists: {Exists}, InputOTP: {InputOTP}, StoredOTP: {StoredOTP}",
            userId, cacheKey, existsBefore, otp, storedOtp ?? "NULL");

        if (existsBefore && storedOtp == otp)
        {
            _cache.Remove(cacheKey);
            _logger.LogInformation("Email confirmation OTP validated and consumed for user {UserId}", userId);
            return true;
        }

        _logger.LogWarning("Invalid or expired email confirmation OTP for user {UserId}", userId);
        return false;
    }
}
