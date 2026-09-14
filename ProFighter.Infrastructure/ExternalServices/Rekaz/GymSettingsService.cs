using Microsoft.Extensions.Options;
using ProFighter.Application.Common.Interfaces;
using ProFighter.Domain.Enums;

namespace ProFighter.Infrastructure.ExternalServices.Rekaz;

/// <summary>
/// Reads per-gym Rekaz settings (e.g. BranchId) from IOptions&lt;RekazMultiGymOptions&gt;.
/// </summary>
public sealed class GymSettingsService : IGymSettingsService
{
    private readonly RekazMultiGymOptions _options;

    public GymSettingsService(IOptions<RekazMultiGymOptions> options)
    {
        _options = options.Value;
    }

    /// <inheritdoc/>
    public Guid? GetBranchId(GymType gymType) => gymType switch
    {
        GymType.ProFighter => _options.ProFighter?.BranchId,
        GymType.ProGym     => _options.ProGym?.BranchId,
        _                  => null
    };
}
