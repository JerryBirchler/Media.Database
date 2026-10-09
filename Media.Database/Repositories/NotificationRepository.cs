using Media.Common.Helpers.Fluent;
using Media.Common.Transactions;
using Media.Database.Models;
using Media.Database.Repositories.Queries;
using Microsoft.Extensions.Logging;

#pragma warning disable CS8981
using pn = Media.Database.Repositories.Schemas.ParameterNames;
#pragma warning restore CS8981

namespace Media.Database.Repositories;

/// <inheritdoc cref="INotificationRepository"/>
public class NotificationRepository(
    ISqlQueryExecutor sqlExecutor,
    IAuditMessageRepository auditMessageRepository,
    Func<IUnitOfWork> unitOfWorkFactory,
    ILogger<NotificationRepository> logger)
    : INotificationRepository
{
    private readonly ISqlQueryExecutor _sqlExecutor = sqlExecutor;
    private readonly IAuditMessageRepository _auditMessageRepository = auditMessageRepository;
    private readonly Func<IUnitOfWork> _unitOfWorkFactory = unitOfWorkFactory;
    private readonly FluentLogger<NotificationRepository> _logger = logger.Initializer();

    public async Task<int> CreateAsync(long messageId, IReadOnlyCollection<int> recipientPersonIds, DateTimeOffset? expiresOn = null)
    {
        if (recipientPersonIds.Count == 0)
            return 0;

        try
        {
            var made = await _sqlExecutor.QuerySingleValueAsync
            (
                QueryNotifications.CreateSql,
                p =>
                {
                    p.AddWithValue(pn.MessageId, messageId);
                    p.AddWithValue(pn.RecipientPersonIds, recipientPersonIds.ToArray());
                    p.AddWithValue(pn.ExpiresOn, (object?)expiresOn ?? DBNull.Value);
                },
                reader => reader.GetInt64(0)
            );

            return (int)(made ?? 0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CreateAsync failed for MessageId: [{MessageId}], Recipients: [{Count}]", messageId, recipientPersonIds.Count);
            throw;
        }
    }

    public async Task<NotificationView?> GetForRecipientAsync(Guid notificationUuid, int recipientPersonId)
    {
        try
        {
            return await _sqlExecutor.QuerySingleAsync
            (
                QueryNotifications.GetForRecipientSql,
                p =>
                {
                    p.AddWithValue(pn.NotificationUuid, notificationUuid);
                    p.AddWithValue(pn.RecipientPersonId, recipientPersonId);
                },
                reader => reader.ToNotificationView()
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetForRecipientAsync failed for RecipientPersonId: [{RecipientPersonId}]", recipientPersonId);
            throw;
        }
    }

    public async Task<List<NotificationView>> ListOpenAsync(int recipientPersonId, long? before, int limit)
    {
        try
        {
            return await _sqlExecutor.QueryManyAsync
            (
                QueryNotifications.ListOpenByRecipientSql,
                p =>
                {
                    p.AddWithValue(pn.RecipientPersonId, recipientPersonId);
                    p.AddWithValue(pn.NotificationId, (object?)before ?? DBNull.Value);
                    p.AddWithValue(pn.Limit, limit);
                },
                reader => reader.ToNotificationView()
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ListOpenAsync failed for RecipientPersonId: [{RecipientPersonId}]", recipientPersonId);
            throw;
        }
    }

    public async Task<bool> SetStatusAsync(Guid notificationUuid, int recipientPersonId, NotificationStatus status)
    {
        var kind = status switch
        {
            NotificationStatus.Seen => AuditKinds.NotificationSeen,
            NotificationStatus.Dismissed => AuditKinds.NotificationDismissed,
            NotificationStatus.Acted => AuditKinds.NotificationActed,
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, "A recipient may only mark a notification seen, dismissed or acted.")
        };

        await using var uow = _unitOfWorkFactory();

        try
        {
            await uow.BeginTransactionAsync();

            var notificationId = await _sqlExecutor.QuerySingleValueAsync
            (
                uow,
                QueryNotifications.SetStatusSql,
                p =>
                {
                    p.AddWithValue(pn.NotificationUuid, notificationUuid);
                    p.AddWithValue(pn.RecipientPersonId, recipientPersonId);
                    p.AddWithValue(pn.Status, (int)status);
                    p.AddWithValue(pn.UpdatedOn, DateTimeOffset.UtcNow);
                },
                reader => reader.GetInt64(0)
            );

            // The change and its record commit together; nothing changed records nothing.
            if (notificationId is { } id)
                await _auditMessageRepository.RecordAsync(uow, null, new AuditEntry(kind, recipientPersonId, recipientPersonId, NotificationId: id));

            await uow.CommitAsync();
            return notificationId is not null;
        }
        catch (Exception ex)
        {
            await uow.RollbackAsync();
            _logger.LogError(ex, "SetStatusAsync failed for RecipientPersonId: [{RecipientPersonId}], Status: [{Status}]", recipientPersonId, status);
            throw;
        }
    }
}
