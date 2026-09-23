namespace ProFighter.Application.Common.Interfaces;

public interface IExceptionLogFormatter
{
    string ToOneLine(Exception? ex);
}
