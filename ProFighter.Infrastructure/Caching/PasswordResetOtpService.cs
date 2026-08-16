using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using ProFighter.Application.Common.Interfaces;

namespace ProFighter.Infrastructure.Caching;

public class PasswordResetOtpService : IPasswordResetOtpService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<PasswordResetOtpService> _logger;

    public PasswordResetOtpService(
        IMemoryCache cache,
        ILogger<PasswordResetOtpService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public void StoreOtp(string userId, string otp, TimeSpan expiry)
    {
        var cacheKey = $"password-reset-otp:{userId}";
        var options = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiry
        };

        _cache.Set(cacheKey, otp, options);
        
        // Verify storage by trying to retrieve immediately
        var testRetrieve = _cache.Get<string>(cacheKey);
        _logger.LogInformation("Stored password reset OTP for user {UserId}, OTP: {Otp}, Key: {Key}, Expiry: {Minutes}min, Retrieved: {Retrieved}",
            userId, otp, cacheKey, expiry.TotalMinutes, testRetrieve ?? "NULL");
    }

    public bool ValidateAndConsumeOtp(string userId, string otp)
    {
        var cacheKey = $"password-reset-otp:{userId}";
        
        // Log attempt details
        var existsBefore = _cache.TryGetValue(cacheKey, out string? storedOtp);
        _logger.LogInformation("OTP Validation attempt - UserId: {UserId}, Key: {Key}, Exists: {Exists}, InputOTP: {InputOTP}, StoredOTP: {StoredOTP}",
            userId, cacheKey, existsBefore, otp, storedOtp ?? "NULL");

        if (existsBefore && storedOtp == otp)
        {
            _cache.Remove(cacheKey);
            _logger.LogInformation("Password reset OTP validated and consumed for user {UserId}", userId);
            return true;
        }

        _logger.LogWarning("Invalid or expired password reset OTP for user {UserId}", userId);
        return false;
    }
}
