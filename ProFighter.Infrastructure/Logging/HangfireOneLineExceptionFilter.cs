using Hangfire.Common;
using Hangfire.Logging;
using Hangfire.States;
using Microsoft.Extensions.Logging;

namespace ProFighter.Infrastructure.Logging;

public class HangfireOneLineExceptionFilter : JobFilterAttribute, IElectStateFilter
{
    private static ILogger? _logger;

    public static void Initialize(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<HangfireOneLineExceptionFilter>();
    }

    public void OnStateElection(ElectStateContext context)
    {
        if (context.CandidateState is FailedState failedState)
        {
            var ex = failedState.Exception;
            var jobId = context.BackgroundJob.Id;
            var oneLineError = ExceptionLogFormatter.ToOneLine(ex);

            _logger?.LogWarning("Job {JobId} failed: {OneLineError}", jobId, oneLineError);
        }
    }
}
