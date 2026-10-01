using Media.Common.Helpers.Fluent;
using Media.Database.Models;
using Media.Database.Repositories.Queries;
using Microsoft.Extensions.Logging;
using pn = Media.Database.Repositories.Schemas.ParameterNames;

namespace Media.Database.Repositories;

/// <inheritdoc cref="IKeyProvisioningRepository"/>
public class KeyProvisioningRepository(
    ISqlQueryExecutor sqlExecutor,
    ILogger<KeyProvisioningRepository> logger) : IKeyProvisioningRepository
{
    private readonly ISqlQueryExecutor _sqlExecutor = sqlExecutor;
    private readonly FluentLogger<KeyProvisioningRepository> _logger = logger.Initializer();

    public async Task<ProvisioningCandidate?> GetCandidateAsync(int sourceMachineId)
    {
        try
        {
            return await _sqlExecutor.QuerySingleAsync(
                QueryRegistrations.GetProvisioningCandidateSql,
                p => p.AddWithValue(pn.SourceMachineId, sourceMachineId),
                reader => reader.ToProvisioningCandidate());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetCandidateAsync failed for SourceMachineId: [{SourceMachineId}]", sourceMachineId);
            throw;
        }
    }

    public async Task<List<ProvisioningCandidate>> GetCandidatesAsync(int limit)
    {
        try
        {
            return await _sqlExecutor.QueryManyAsync(
                QueryRegistrations.GetProvisioningCandidatesSql,
                p => p.AddWithValue(pn.Limit, limit),
                reader => reader.ToProvisioningCandidate());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetCandidatesAsync failed. Limit: [{Limit}]", limit);
            throw;
        }
    }

    public async Task<bool> BindGroupShellIfUnboundAsync(int sourceMachineId, int groupShellId)
    {
        try
        {
            return 1 == await _sqlExecutor.ExecuteAsync(
                QueryRegistrations.SetGroupShellIdIfUnsetSql,
                p =>
                {
                    p.AddWithValue(pn.SourceMachineId, sourceMachineId);
                    p.AddWithValue(pn.GroupShellId, groupShellId);
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "BindGroupShellIfUnboundAsync failed for SourceMachineId: [{SourceMachineId}], GroupShellId: [{GroupShellId}]", sourceMachineId, groupShellId);
            throw;
        }
    }
}
