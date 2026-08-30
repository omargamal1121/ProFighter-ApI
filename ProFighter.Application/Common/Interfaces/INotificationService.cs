using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ProFighter.Application.Common.Interfaces;

public interface INotificationService
{
    Task SendToUserAsync(Guid userId, string title, string body, Dictionary<string, string>? data = null, CancellationToken ct = default);
}
