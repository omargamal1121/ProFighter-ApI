using System.Diagnostics;
using System.Text.RegularExpressions;

namespace ProFighter.Infrastructure.Logging;

public class ExceptionLogFormatter : Application.Common.Interfaces.IExceptionLogFormatter
{
    private static readonly Regex NewlineRegex = new Regex(@"[\r\n]+", RegexOptions.Compiled);

    string Application.Common.Interfaces.IExceptionLogFormatter.ToOneLine(Exception? ex) => ToOneLine(ex);

    public static string ToOneLine(Exception? ex)
    {
        if (ex == null)
            return string.Empty;

        // 1. Unwrap to find innermost non-null exception
        var innerEx = ex;
        while (innerEx.InnerException != null)
        {
            innerEx = innerEx.InnerException;
        }

        var exceptionType = innerEx.GetType().Name;
        var message = innerEx.Message ?? string.Empty;

        // Clean message: replace \r or \n with single space and trim
        message = NewlineRegex.Replace(message, " ").Trim();

        // 2. Location matching: first stack frame belonging to ProFighter.
        string location = string.Empty;
        try
        {
            var stackTrace = new StackTrace(ex, true);
            var frames = stackTrace.GetFrames();
            if (frames != null)
            {
                foreach (var frame in frames)
                {
                    var method = frame.GetMethod();
                    var declaringType = method?.DeclaringType;
                    if (declaringType != null && declaringType.FullName != null &&
                        declaringType.FullName.StartsWith("ProFighter.", StringComparison.OrdinalIgnoreCase))
                    {
                        var fileName = frame.GetFileName();
                        var line = frame.GetFileLineNumber();
                        var methodName = method?.Name ?? "Unknown";

                        if (!string.IsNullOrEmpty(fileName))
                        {
                            fileName = System.IO.Path.GetFileName(fileName);
                            location = $" @ {fileName}:{line} in {methodName}";
                        }
                        else
                        {
                            location = $" @ {declaringType.Name}.{methodName}";
                        }
                        break;
                    }
                }
            }
        }
        catch
        {
            // Ignore stack trace extraction failures
        }

        return $"{exceptionType}: {message}{location}";
    }
}
