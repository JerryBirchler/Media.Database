using Media.Common.Helpers.Fluent;
using Media.Common.Transactions;
using Media.Database.Mappers;
using Media.Database.Models;
using Media.Database.Repositories.Queries;
using Media.Database.Repositories.Queries.Helpers;
using Microsoft.Extensions.Logging;

#pragma warning disable CS8981
using pn = Media.Database.Repositories.Schemas.ParameterNames;
#pragma warning restore CS8981

namespace Media.Database.Repositories;

/// <inheritdoc cref="IGroupSourceMachineRepository"/>
public class GroupSourceMachineRepository(
    ISqlQueryExecutor sqlExecutor,
    IMapGroupSourceMachineResponse groupSourceMachineResponseMapper,
    IAuditMessageRepository auditMessageRepository,
    Func<IUnitOfWork> unitOfWorkFactory,
    ILogger<GroupSourceMachineRepository> logger)
    : IGroupSourceMachineRepository
{
    private readonly ISqlQueryExecutor _sqlExecutor = sqlExecutor;
    private readonly IMapGroupSourceMachineResponse _groupSourceMachineResponseMapper = groupSourceMachineResponseMapper;
    private readonly IAuditMessageRepository _auditMessageRepository = auditMessageRepository;
    private readonly Func<IUnitOfWork> _unitOfWorkFactory = unitOfWorkFactory;
    private readonly FluentLogger<GroupSourceMachineRepository> _logger = logger.Initializer();

    public async Task<GroupSourceMachine> UpsertAsync(int groupId, int sourceMachineId, AuditEntry audit) =>
        // UpsertSql's update/insert CTE pair always produces exactly one row between them.
        (await AuditedAsync(QueryGroupsSourceMachines.UpsertSql, nameof(UpsertAsync), groupId, sourceMachineId, audit))!;

    public async Task<GroupSourceMachine?> GetActiveBySourceMachineIdAsync(int sourceMachineId)
    {
        try
        {
            return await _sqlExecutor.QuerySingleAsync
            (
                QueryGroupsSourceMachines.GetActiveBySourceMachineIdSql,
                p => p.AddWithValue(pn.SourceMachineId, sourceMachineId),
                reader => reader.ToGroupSourceMachine(_groupSourceMachineResponseMapper)
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetActiveBySourceMachineIdAsync failed for SourceMachineId: [{SourceMachineId}]", sourceMachineId);
            throw;
        }
    }

    public Task<GroupSourceMachine?> DeactivateAsync(int groupId, int sourceMachineId, AuditEntry audit) =>
        AuditedAsync(QueryGroupsSourceMachines.DeactivateSql, nameof(DeactivateAsync), groupId, sourceMachineId, audit);

    /// <summary>
    /// A change to a group/device association and its audit entry, in one transaction (DATABASE-74):
    /// the entry is recorded only when a row changed.
    /// </summary>
    private async Task<GroupSourceMachine?> AuditedAsync(string sql, string operation, int groupId, int sourceMachineId, AuditEntry audit)
    {
        await using var uow = _unitOfWorkFactory();

        try
        {
            await uow.BeginTransactionAsync();

            var changed = await _sqlExecutor.QuerySingleAsync
            (
                uow,
                sql,
                p =>
                {
                    p.AddWithValue(pn.GroupId, groupId);
                    p.AddWithValue(pn.SourceMachineId, sourceMachineId);
                    p.AddWithValue(pn.UpdatedOn, DateTimeOffset.UtcNow);
                },
                reader => reader.ToGroupSourceMachine(_groupSourceMachineResponseMapper)
            );

            if (changed is not null)
                await _auditMessageRepository.RecordAsync(uow, groupId, audit);

            await uow.CommitAsync();
            return changed;
        }
        catch (Exception ex)
        {
            await uow.RollbackAsync();
            _logger.LogError(ex, "{Operation} failed for GroupId: [{GroupId}], SourceMachineId: [{SourceMachineId}], Kind: [{Kind}]", operation, groupId, sourceMachineId, audit.Kind);
            throw;
        }
    }

    public Task<List<(int SourceMachineId, string SourceMachineName)>> GetSourceMachineIdentifiersByGroupIdAsync(int groupId, bool includeInactive, (int SourceMachineId, string SourceMachineName)? next, int limit) =>
        GetSourceMachineIdentifiersByGroupIdAsync(groupId, includeInactive, includeRemoved: false, next, limit);

    public async Task<List<(int SourceMachineId, string SourceMachineName)>> GetSourceMachineIdentifiersByGroupIdAsync(int groupId, bool includeInactive, bool includeRemoved, (int SourceMachineId, string SourceMachineName)? next, int limit)
    {
        try
        {
            var afterSourceMachineName = next?.SourceMachineName;
            var afterSourceMachineId = next?.SourceMachineId;

            return await _sqlExecutor.QueryManyAsync(
                QueryGroupsSourceMachines.GetSourceMachineIdentifiersByGroupIdSql,
                p =>
                {
                    p.AddWithValue(pn.GroupId, groupId);
                    p.AddWithValue(pn.IncludeInactive, includeInactive);
                    p.AddWithValue(pn.IncludeRemoved, includeRemoved);
                    p.AddWithValue(pn.SourceMachineName, afterSourceMachineName.ToNullableValueForSql());
                    p.AddWithValue(pn.SourceMachineId, afterSourceMachineId.ToNullableValueForSql());
                    p.AddWithValue(pn.Limit, limit);
                },
                reader => reader.ToSourceMachineIdentifier());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetSourceMachineIdentifiersByGroupIdAsync failed for GroupId: [{GroupId}]", groupId);
            throw;
        }
    }

    public async Task<int?> GetActiveSourceMachineIdByGroupAndDisambiguationAsync(int groupId, string sourceMachineName, DeviceTypes deviceType, string disambiguationKey)
    {
        try
        {
            return await _sqlExecutor.QuerySingleValueAsync(
                QueryGroupsSourceMachines.GetActiveSourceMachineIdByGroupAndDisambiguationSql,
                p =>
                {
                    p.AddWithValue(pn.GroupId, groupId);
                    p.AddWithValue(pn.SourceMachineName, sourceMachineName);
                    p.AddWithValue(pn.DeviceTypeId, (int)deviceType);
                    p.AddWithValue(pn.DisambiguationKey, disambiguationKey);
                },
                reader => reader.GetInt32(0));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetActiveSourceMachineIdByGroupAndDisambiguationAsync failed for GroupId: [{GroupId}], SourceMachineName: [{SourceMachineName}]", groupId, sourceMachineName);
            throw;
        }
    }
}
