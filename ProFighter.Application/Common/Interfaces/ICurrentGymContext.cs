using ProFighter.Domain.Enums;

namespace ProFighter.Application.Common.Interfaces;

public interface ICurrentGymContext
{
    GymType CurrentGymType { get; }
}
