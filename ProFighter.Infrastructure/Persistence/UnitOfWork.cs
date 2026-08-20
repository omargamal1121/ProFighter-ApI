using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProFighter.Application.Common.Interfaces;

namespace ProFighter.Infrastructure.Persistence;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;
    private readonly ILogger<UnitOfWork> _logger;

    public UnitOfWork(AppDbContext context, ILogger<UnitOfWork> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<TResult> ExecuteInTransactionAsync<TResult>(
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken ct = default)
    {
        var strategy = _context.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            // Check if there's already a transaction (nested transaction support)
            if (_context.Database.CurrentTransaction is not null)
            {
                return await ExecuteWithExceptionTranslation(operation, ct);
            }

            await using var transaction = await _context.Database.BeginTransactionAsync(ct);
            try
            {
                var result = await ExecuteWithExceptionTranslation(operation, ct);
                await _context.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Transaction failed, rolling back.");
                await transaction.RollbackAsync(ct);
                throw;
            }
        });
    }

    private async Task<TResult> ExecuteWithExceptionTranslation<TResult>(
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken ct)
    {
        try
        {
            return await operation(ct);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogError(ex, "Concurrency conflict occurred while saving changes.");
            throw new InvalidOperationException(
                "Unable to save changes. The record was modified or deleted by another process. Please refresh and try again.", ex);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Database update error occurred.");
            var innerMessage = ex.InnerException?.Message?.ToLowerInvariant() ?? string.Empty;

            if (innerMessage.Contains("foreign key") || innerMessage.Contains("reference"))
                throw new InvalidOperationException("Cannot save changes. The referenced record does not exist.", ex);

            if (innerMessage.Contains("unique") || innerMessage.Contains("duplicate"))
                throw new InvalidOperationException("Cannot save changes. A record with this value already exists.", ex);

            if (innerMessage.Contains("check constraint"))
                throw new InvalidOperationException("Cannot save changes. Data validation failed.", ex);

            throw new InvalidOperationException("An error occurred while saving to the database. Please check your data and try again.", ex);
        }
    }
}
