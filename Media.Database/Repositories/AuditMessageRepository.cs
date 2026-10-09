using Media.Common.Helpers.Fluent;
using Media.Common.Transactions;
using Media.Database.Models;
using Media.Database.Repositories.Queries;
using Microsoft.Extensions.Logging;

#pragma warning disable CS8981
using pn = Media.Database.Repositories.Schemas.ParameterNames;
#pragma warning restore CS8981

namespace Media.Database.Repositories;

/// <inheritdoc cref="IAuditMessageRepository"/>
public class AuditMessageRepository(
    ISqlQueryExecutor sqlExecutor,
    ILogger<AuditMessageRepository> logger)
    : IAuditMessageRepository
{
    private readonly ISqlQueryExecutor _sqlExecutor = sqlExecutor;
    private readonly FluentLogger<AuditMessageRepository> _logger = logger.Initializer();

    public async Task<long> RecordAsync(IUnitOfWork unitOfWork, int? groupId, AuditEntry entry)
    {
        try
        {
            var id = await _sqlExecutor.QuerySingleValueAsync
            (
                unitOfWork,
                QueryAuditMessages.RecordSql,
                p =>
                {
                    p.AddWithValue(pn.Kind, entry.Kind);
                    p.AddWithValue(pn.Parameters, (object?)entry.Parameters ?? DBNull.Value);
                    p.AddWithValue(pn.GroupId, (object?)groupId ?? DBNull.Value);
                    p.AddWithValue(pn.SubjectPersonId, (object?)entry.SubjectPersonId ?? DBNull.Value);
                    p.AddWithValue(pn.ActorPersonId, (object?)entry.ActorPersonId ?? DBNull.Value);
                    p.AddWithValue(pn.NotificationId, (object?)entry.NotificationId ?? DBNull.Value);
                },
                reader => reader.GetInt64(0)
            );

            // RecordSql's two inserts always return the one entry they made.
            return id!.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RecordAsync failed for GroupId: [{GroupId}], Kind: [{Kind}]", groupId, entry.Kind);
            throw;
        }
    }

    public async Task<AuditMessage?> GetByIdAsync(long auditMessageId)
    {
        try
        {
            return await _sqlExecutor.QuerySingleAsync
            (
                QueryAuditMessages.GetByIdSql,
                p => p.AddWithValue(pn.AuditMessageId, auditMessageId),
                reader => reader.ToAuditMessage()
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetByIdAsync failed for AuditMessageId: [{AuditMessageId}]", auditMessageId);
            throw;
        }
    }

    public async Task<long?> GetMessageIdAsync(long auditMessageId)
    {
        try
        {
            return await _sqlExecutor.QuerySingleValueAsync
            (
                QueryAuditMessages.GetMessageIdSql,
                p => p.AddWithValue(pn.AuditMessageId, auditMessageId),
                reader => reader.GetInt64(0)
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetMessageIdAsync failed for AuditMessageId: [{AuditMessageId}]", auditMessageId);
            throw;
        }
    }

    public async Task<List<AuditMessage>> ListByGroupAsync(int groupId, long? before, int limit)
    {
        try
        {
            return await _sqlExecutor.QueryManyAsync
            (
                QueryAuditMessages.ListByGroupSql,
                p =>
                {
                    p.AddWithValue(pn.GroupId, groupId);
                    p.AddWithValue(pn.AuditMessageId, (object?)before ?? DBNull.Value);
                    p.AddWithValue(pn.Limit, limit);
                },
                reader => reader.ToAuditMessage()
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ListByGroupAsync failed for GroupId: [{GroupId}]", groupId);
            throw;
        }
    }
}
