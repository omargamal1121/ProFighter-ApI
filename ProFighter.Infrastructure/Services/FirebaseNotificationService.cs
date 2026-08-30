using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FirebaseAdmin.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProFighter.Application.Common.Interfaces;

namespace ProFighter.Infrastructure.Services;

public class FirebaseNotificationService : INotificationService
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<FirebaseNotificationService> _logger;

    public FirebaseNotificationService(IApplicationDbContext context, ILogger<FirebaseNotificationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task SendToUserAsync(Guid userId, string title, string body, Dictionary<string, string>? data = null, CancellationToken ct = default)
    {
        try
        {
            var tokens = await _context.DeviceTokens
                .Where(t => t.CustomerId == userId)
                .ToListAsync(ct);

            if (!tokens.Any())
            {
                _logger.LogInformation("No device tokens found for user {UserId}. Skipping push notification.", userId);
                return;
            }

            var notification = new Notification { Title = title, Body = body };
            var dataPayload = data ?? new Dictionary<string, string>();
            var fcmTokens = tokens.Select(t => t.FcmToken).ToList();

            var messages = fcmTokens.Select(token => new Message
            {
                Token = token,
                Notification = notification,
                Data = dataPayload
            }).ToList();

            var response = await FirebaseMessaging.DefaultInstance.SendEachAsync(messages, ct);

            if (response.FailureCount > 0)
            {
                var failedTokensToRemove = new List<ProFighter.Domain.Entities.DeviceToken>();

                for (int i = 0; i < response.Responses.Count; i++)
                {
                    var result = response.Responses[i];
                    if (!result.IsSuccess)
                    {
                        var error = result.Exception?.MessagingErrorCode;
                        if (error == MessagingErrorCode.Unregistered || error == MessagingErrorCode.InvalidArgument)
                        {
                            _logger.LogWarning("Token {Token} for user {UserId} is unregistered or invalid. Queueing for deletion.", fcmTokens[i], userId);
                            failedTokensToRemove.Add(tokens[i]);
                        }
                        else
                        {
                            _logger.LogWarning(result.Exception, "Failed to send notification to token {Token} for user {UserId}. Error: {Error}", fcmTokens[i], userId, error);
                        }
                    }
                }

                if (failedTokensToRemove.Any())
                {
                    _context.DeviceTokens.RemoveRange(failedTokensToRemove);
                    await _context.SaveChangesAsync(ct);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while attempting to send a push notification to user {UserId}.", userId);
            // We swallow the exception here because push notifications should not fail the main transaction/webhook.
        }
    }
}
