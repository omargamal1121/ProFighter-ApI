using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ProFighter.Application.Common.Interfaces;
using System.Net;
using System.Text;

namespace ProFighter.Infrastructure.Services;

public class AccountEmailService : IAccountEmailService
{
    private readonly IEmailSender _emailSender;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AccountEmailService> _logger;

    // ==== Brand constants (ProFighter Club) ====
    private const string CompanyName = "ProFighter Club";
    private const string CompanyNameAr = "نادي برو فايتر";
    private const string BrandPrimaryDark = "#111111";   
    private const string BrandPrimaryLight = "#E53935";  
    private const string BrandAccent = "#FFC107";       
    private const string BrandAccentDark = "#FF9800";
    private const string TextDark = "#212121";
    private const string TextMuted = "#757575";

    public AccountEmailService(
        IConfiguration configuration,
        IEmailSender emailSender,
        ILogger<AccountEmailService> logger)
    {
        _configuration = configuration;
        _emailSender = emailSender;
        _logger = logger;
    }

    private string LogoUrl => _configuration["Company:LogoUrl"] ?? "https://via.placeholder.com/84/111111/FFFFFF?text=ProFighter";
    private string FacebookUrl => _configuration["Company:FacebookUrl"] ?? "https://www.facebook.com/";
    private string WebsiteUrl => _configuration["Company:WebsiteUrl"] ?? "https://profighterclub.com";
    private string SupportPhone => _configuration["Company:SupportPhone"] ?? "";

    public async Task SendValidationEmailAsync(string email, string userId, string otp)
    {
        try
        {
            string subject = $"رمز تأكيد البريد الإلكتروني - {CompanyNameAr}";

            string message = CreateEmailTemplate(
                "رمز تأكيد البريد الإلكتروني",
                "Email Confirmation OTP",
                BrandPrimaryDark,
                $@"
            <h1 style='color:{TextDark}; margin:0 0 16px; font-size:22px;'>أهلاً بك في {CompanyNameAr} 👋</h1>
            <p style='font-size:15px; line-height:1.8; color:{TextDark};'>
                شكرًا لتسجيلك معنا. لإتمام إنشاء حسابك، برجاء تأكيد بريدك الإلكتروني باستخدام الرمز التالي.
            </p>
            <div style='background-color:#f0f4f8; padding:24px; border-radius:12px; margin:20px 0; text-align:center; border:2px solid {BrandPrimaryLight};'>
                <h2 style='color:{BrandPrimaryDark}; margin:0 0 8px; font-size:32px; letter-spacing:8px; font-weight:bold;'>{otp}</h2>
                <p style='color:{TextMuted}; margin:0; font-size:14px;'>رمز التحقق لمرة واحدة (OTP)</p>
            </div>
            {NoticeBlock("ملحوظة هامة", "هذا الرمز صالح لمدة 15 دقيقة فقط لدواعي الأمان.")}
            <p style='font-size:13px; color:{TextMuted};'>
                إذا لم تقم بإنشاء هذا الحساب، برجاء تجاهل هذه الرسالة.
            </p>"
            );

            await _emailSender.SendEmailAsync(email, subject, message);
            _logger.LogInformation($"Email confirmation OTP sent successfully to {email}");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to send email confirmation OTP to {email}: {ex.Message}");
            throw;
        }
    }

    public async Task SendEmailAfterChangePassAsync(string username, string email)
    {
        string subject = $"تم تغيير كلمة المرور - {CompanyNameAr}";
        string message = CreateEmailTemplate(
            "تم تغيير كلمة المرور",
            "Password Changed",
            BrandPrimaryDark,
            $@"
            <h1 style='color:{TextDark}; margin:0 0 16px; font-size:22px;'>تم تغيير كلمة المرور بنجاح</h1>
            <p style='font-size:15px; line-height:1.8; color:{TextDark};'>
                مرحبًا <strong>{username}</strong>،<br>
                نود إعلامك بأنه تم تغيير كلمة المرور الخاصة بحسابك مؤخرًا.
            </p>
            {NoticeBlock("تنبيه أمني", "إذا لم تقم بهذا التغيير، برجاء إعادة تعيين كلمة المرور فورًا والتواصل مع فريق الدعم.", isWarning: true)}
            <p style='font-size:13px; color:{TextMuted};'>
                نهتم بأمان حسابك، برجاء الحفاظ على بيانات الدخول الخاصة بك.
            </p>"
        );
        await _emailSender.SendEmailAsync(email, subject, message);
    }

    public async Task SendPasswordResetEmailAsync(string email, string username, string otp)
    {
        try
        {
            string subject = $"رمز إعادة تعيين كلمة المرور - {CompanyNameAr}";

            string message = CreateEmailTemplate(
                "رمز إعادة تعيين كلمة المرور",
                "Password Reset OTP",
                BrandPrimaryDark,
                $@"
            <h1 style='color:{TextDark}; margin:0 0 16px; font-size:22px;'>رمز إعادة تعيين كلمة المرور</h1>
            <p style='font-size:15px; line-height:1.8; color:{TextDark};'>
                مرحبًا <strong>{username}</strong>،<br>
                وصلنا طلب لإعادة تعيين كلمة المرور الخاصة بحسابك. إذا كنت أنت من طلب ذلك، استخدم الرمز التالي لتعيين كلمة مرور جديدة.
            </p>
            <div style='background-color:#f0f4f8; padding:24px; border-radius:12px; margin:20px 0; text-align:center; border:2px solid {BrandPrimaryLight};'>
                <h2 style='color:{BrandPrimaryDark}; margin:0 0 8px; font-size:32px; letter-spacing:8px; font-weight:bold;'>{otp}</h2>
                <p style='color:{TextMuted}; margin:0; font-size:14px;'>رمز التحقق لمرة واحدة (OTP)</p>
            </div>
            {NoticeBlock("تنبيه أمني", "هذا الرمز صالح لمدة 15 دقيقة فقط. إذا لم تطلب ذلك، تجاهل هذه الرسالة.", isWarning: true)}
            <p style='font-size:13px; color:{TextMuted};'>
                لأمانك، إذا لم تطلب إعادة تعيين كلمة المرور، برجاء التواصل مع فريق الدعم فورًا.
            </p>"
            );

            await _emailSender.SendEmailAsync(email, subject, message);
            _logger.LogInformation($"Password reset OTP email sent successfully to {email}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to send password reset OTP email to {email}");
            throw;
        }
    }

    public async Task SendPasswordResetSuccessEmailAsync(string email)
    {
        try
        {
            string subject = $"تم إعادة تعيين كلمة المرور بنجاح - {CompanyNameAr}";
            string message = CreateEmailTemplate(
                "تم إعادة التعيين بنجاح",
                "Password Reset Successful",
                BrandPrimaryDark,
                $@"
                <div style='background-color:#fff3f3; padding:18px; border-radius:10px; margin:0 0 20px; border-inline-start:4px solid {BrandPrimaryLight};'>
                    <h3 style='color:{BrandPrimaryDark}; margin:0 0 8px; font-size:17px;'>✅ تم تغيير كلمة المرور بنجاح!</h3>
                    <p style='color:{TextDark}; margin:0; font-size:14px;'>
                        حسابك الآن مؤمَّن بكلمة المرور الجديدة.
                    </p>
                </div>
                <p style='font-size:15px; line-height:1.8; color:{TextDark};'>
                    إذا كنت أنت من قام بهذا التغيير، لا داعي لأي إجراء إضافي، ويمكنك تسجيل الدخول بكلمة المرور الجديدة الآن.
                </p>
                {NoticeBlock("لم تقم بهذا الإجراء؟", "برجاء إعادة تعيين كلمة المرور فورًا والتواصل مع فريق الدعم، ومراجعة حسابك لأي نشاط غير معتاد.", isWarning: true)}
                <p style='font-size:13px; color:{TextMuted};'>
                    شكرًا لثقتك في {CompanyNameAr}.
                </p>"
            );

            await _emailSender.SendEmailAsync(email, subject, message);
            _logger.LogInformation($"Password reset success email sent successfully to {email}");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to send password reset success email to {email}: {ex.Message}");
        }
    }

    public async Task SendAccountLockedEmailAsync(string email, string username, string reason = "محاولات دخول فاشلة متكررة")
    {
        try
        {
            string subject = $"تم إيقاف الحساب مؤقتًا - {CompanyNameAr}";
            string message = CreateEmailTemplate(
                "تم إيقاف الحساب مؤقتًا",
                "Account Locked",
                BrandPrimaryDark,
                $@"
                <div style='background-color:#fdecea; padding:18px; border-radius:10px; margin:0 0 20px; border-inline-start:4px solid #d64545;'>
                    <h3 style='color:#b02a2a; margin:0 0 8px; font-size:17px;'>🔒 تم إيقاف حسابك مؤقتًا</h3>
                    <p style='color:#7a2323; margin:0; font-size:14px;'>السبب: {reason}</p>
                </div>
                <p style='font-size:15px; line-height:1.8; color:{TextDark};'>
                    مرحبًا <strong>{username}</strong>،<br>
                    لأمان حسابك، تم إيقافه مؤقتًا نتيجة نشاط غير معتاد.
                </p>
                <div style='background-color:#f4f4f4; padding:16px; border-radius:10px; margin:0 0 20px;'>
                    <h4 style='color:{TextDark}; margin:0 0 10px; font-size:15px;'>الخطوات التالية:</h4>
                    <ol style='color:{TextMuted}; margin:0; padding-inline-start:20px; font-size:14px; line-height:1.8;'>
                        <li>انتظر انتهاء فترة الإيقاف (عادةً 15 دقيقة)</li>
                        <li>استخدم خاصية إعادة تعيين كلمة المرور إذا لزم الأمر</li>
                        <li>تواصل مع فريق الدعم إذا استمرت المشكلة</li>
                    </ol>
                </div>
                <p style='font-size:13px; color:{TextMuted};'>
                    إذا لم يكن هذا أنت، برجاء التواصل مع فريق الدعم فورًا لتأمين حسابك.
                </p>"
            );

            await _emailSender.SendEmailAsync(email, subject, message);
            _logger.LogInformation($"Account locked email sent successfully to {email}");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to send account locked email to {email}: {ex.Message}");
            throw;
        }
    }

    public async Task SendWelcomeEmailAsync(string email, string username)
    {
        try
        {
            string subject = $"أهلاً بك في {CompanyNameAr} 🎉";
            string message = CreateEmailTemplate(
                "أهلاً بك",
                "Welcome",
                BrandPrimaryDark,
                $@"
                <div style='background:linear-gradient(135deg, {BrandPrimaryDark} 0%, {BrandPrimaryLight} 100%); padding:24px; border-radius:12px; margin:0 0 22px; text-align:center;'>
                    <h3 style='color:#ffffff; margin:0 0 10px; font-size:19px;'>🎉 حسابك جاهز الآن!</h3>
                    <p style='color:#f8d7da; margin:0; font-size:15px;'>
                        أهلاً بك <strong>{username}</strong>، سعداء بانضمامك إلى عائلة {CompanyNameAr}
                    </p>
                </div>
                <p style='font-size:15px; line-height:1.8; color:{TextDark};'>
                    شكرًا لانضمامك إلينا! تم إنشاء حسابك بنجاح وهو جاهز للاستخدام الآن.
                </p>
                <div style='background-color:#fffdf7; padding:16px; border-radius:10px; margin:0 0 20px; border-inline-start:4px solid {BrandAccent};'>
                    <h4 style='color:{BrandAccentDark}; margin:0 0 10px; font-size:15px;'>الخطوات التالية:</h4>
                    <ul style='color:{TextDark}; margin:0; padding-inline-start:20px; font-size:14px; line-height:1.8;'>
                        <li>أكمل بيانات ملفك الشخصي</li>
                        <li>تصفح برامجنا الرياضية كالملاكمة والسباحة وغيرها</li>
                        <li>احجز حصصك التدريبية الأولى</li>
                    </ul>
                </div>
                <p style='font-size:13px; color:{TextMuted};'>
                    فريق الدعم لدينا جاهز دائمًا لمساعدتك.
                </p>"
            );

            await _emailSender.SendEmailAsync(email, subject, message);
            _logger.LogInformation($"Welcome email sent successfully to {email}");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to send welcome email to {email}: {ex.Message}");
            throw;
        }
    }

    // ==================== Shared UI building blocks ====================

    private string ButtonBlock(string label, string link)
    {
        return $@"
            <div style='text-align:center; margin:28px 0;'>
                <a href='{link}'
                   style='background:linear-gradient(135deg, {BrandAccent} 0%, {BrandAccentDark} 100%); color:#111111; padding:14px 32px;
                          border-radius:8px; text-decoration:none; font-size:15px; font-weight:700; display:inline-block;
                          box-shadow:0 4px 10px rgba(255,193,7,0.35);'>
                    {label}
                </a>
            </div>";
    }

    private string NoticeBlock(string title, string body, bool isWarning = false)
    {
        var bg = isWarning ? "#fff8e6" : "#fbe9e7";
        var border = isWarning ? BrandAccent : BrandPrimaryLight;
        var titleColor = isWarning ? BrandAccentDark : BrandPrimaryDark;
        return $@"
            <div style='background-color:{bg}; padding:14px 16px; border-radius:8px; margin:20px 0; border-inline-start:4px solid {border};'>
                <p style='margin:0; color:{TextDark}; font-size:13.5px; line-height:1.7;'>
                    <strong style='color:{titleColor};'>{title}:</strong> {body}
                </p>
            </div>";
    }

    private string CreateEmailTemplate(string titleAr, string titleEn, string accentColor, string content)
    {
        return $@"
            <!DOCTYPE html>
            <html lang='ar' dir='rtl'>
            <head>
                <meta charset='UTF-8'>
                <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                <title>{titleAr} | {CompanyName}</title>
            </head>
            <body style='margin:0; padding:0; font-family:Tahoma, Arial, sans-serif; background-color:#f4f4f4;'>
                <div style='max-width:600px; margin:0 auto; background-color:#ffffff;'>
                    <div style='background:linear-gradient(135deg, {BrandPrimaryDark} 0%, {BrandPrimaryLight} 100%); padding:28px 20px; text-align:center;'>
                        <img src='{LogoUrl}' alt='{CompanyName}' width='84' height='84'
                             style='display:block; margin:0 auto 12px; border-radius:50%; background:#ffffff; padding:4px;' />
                        <h1 style='color:#ffffff; margin:0; font-size:20px; letter-spacing:0.5px;'>{CompanyNameAr}</h1>
                        <p style='color:#ffcdd2; margin:4px 0 0; font-size:12.5px; letter-spacing:1px; text-transform:uppercase;'>
                            Boxing, Swimming &amp; Sports Club · نادي للملاكمة والسباحة والألعاب الرياضية
                        </p>
                    </div>
                    <div style='height:4px; background:linear-gradient(90deg, {BrandAccent}, {BrandAccentDark}, {BrandAccent});'></div>
                    <div style='padding:32px 28px; text-align:right;'>
                        {content}
                    </div>
                    <div style='background-color:#f9f9f9; padding:22px 20px; text-align:center; border-top:1px solid #eeeeee;'>
                        <p style='margin:0 0 8px; color:{TextDark}; font-size:13px; font-weight:bold;'>{CompanyNameAr}</p>
                        <p style='margin:0 0 12px; color:{TextMuted}; font-size:12px;'>
                            {(string.IsNullOrWhiteSpace(SupportPhone) ? "" : $"هاتف الدعم: {SupportPhone} &nbsp;|&nbsp; ")}
                            <a href='{WebsiteUrl}' style='color:{BrandPrimaryDark}; text-decoration:none;'>{WebsiteUrl}</a>
                        </p>
                        <p style='margin:0 0 14px;'>
                            <a href='{FacebookUrl}' style='color:{BrandPrimaryDark}; text-decoration:none; font-size:12px;'>تابعنا على فيسبوك</a>
                        </p>
                        <hr style='border:none; border-top:1px solid #eeeeee; margin:14px 0;'>
                        <p style='margin:0; color:#999999; font-size:11px;'>هذه رسالة تلقائية، برجاء عدم الرد عليها.</p>
                        <p style='margin:4px 0 0; color:#999999; font-size:11px;'>&copy; {DateTime.UtcNow.Year} {CompanyName}. All rights reserved.</p>
                    </div>
                </div>
            </body>
            </html>";
    }
}
