using Media.Common.Email;
using Media.Common.Helpers.Fluent;
using Media.Common.Sms;
using Media.Database.Models;
using Microsoft.Extensions.Logging;

namespace Media.Database.Services;

/// <inheritdoc cref="IGroupKeyDeliveryService"/>
public class GroupKeyDeliveryService(
    IEmailSender emailSender,
    ISmsSender smsSender,
    ILogger<GroupKeyDeliveryService> logger) : IGroupKeyDeliveryService
{
    private const string Subject = "Your Media Organizer encryption key";

    private readonly IEmailSender _emailSender = emailSender;
    private readonly ISmsSender _smsSender = smsSender;
    private readonly FluentLogger<GroupKeyDeliveryService> _logger = logger.Initializer();

    public async Task DeliverAsync(KeyDeliveryMethods keyDeliveryMethod, string emailAddress, string cellPhoneNumber, string rawKey, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rawKey);

        switch (keyDeliveryMethod)
        {
            case KeyDeliveryMethods.Sms:
                if (string.IsNullOrWhiteSpace(cellPhoneNumber))
                    throw new KeyDeliveryUnavailableException("SMS delivery was chosen but this registration has no cell phone number.");

                await _smsSender.SendAsync(cellPhoneNumber, SmsBody(rawKey), cancellationToken);
                break;

            case KeyDeliveryMethods.Email:
                if (string.IsNullOrWhiteSpace(emailAddress))
                    throw new KeyDeliveryUnavailableException("Email delivery was chosen but this registration has no email address.");

                await _emailSender.SendAsync(emailAddress, Subject, HtmlBody(rawKey), TextBody(rawKey), cancellationToken);
                break;

            default:
                throw new InvalidOperationException($"Unhandled {nameof(KeyDeliveryMethods)}: {keyDeliveryMethod}");
        }

        // Records the channel only. The key itself is never logged -- it is not stored anywhere
        // else either, so a log line would be the only copy in the system.
        _logger.LogInformation("Group encryption key delivered. KeyDeliveryMethod: [{KeyDeliveryMethod}]", keyDeliveryMethod);
    }

    private static string SmsBody(string rawKey) =>
        $"Your Media Organizer encryption key is {rawKey} -- keep it somewhere safe. It cannot be recovered or resent, only replaced.";

    private static string TextBody(string rawKey) =>
        $"""
        Your Media Organizer encryption key is:

        {rawKey}

        Keep it somewhere safe. It is not stored on our servers, so it cannot be recovered or
        resent -- only replaced, which makes anything already encrypted under it unreadable.
        """;

    private static string HtmlBody(string rawKey) =>
        $"""
        <p>Your Media Organizer encryption key is:</p>
        <p><strong>{rawKey}</strong></p>
        <p>
            Keep it somewhere safe. It is not stored on our servers, so it cannot be recovered or
            resent &mdash; only replaced, which makes anything already encrypted under it unreadable.
        </p>
        """;
}
