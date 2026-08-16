namespace ProFighter.Application.Common.Constants;

public static class ValidationPatterns
{
    // Saudi mobile numbers only: exactly "966" followed by exactly 9 digits.
    // Total 12 digits, no leading "+", no spaces, no separators. Example: 966591974376
    public const string SaudiMobileNumberPattern = @"^966\d{9}$";
}
