using Media.Common.Helpers.Fluent;
using Media.Database.Mappers;
using Media.Database.Models;
using Media.Database.Repositories.Queries;
using Microsoft.Extensions.Logging;

#pragma warning disable CS8981
using pn = Media.Database.Repositories.Schemas.ParameterNames;
#pragma warning restore CS8981

namespace Media.Database.Repositories;

/// <inheritdoc cref="IPersonSourceMachineRepository"/>
public class PersonSourceMachineRepository(
    ISqlQueryExecutor sqlExecutor,
    IMapPersonSourceMachineResponse personSourceMachineResponseMapper,
    ILogger<PersonSourceMachineRepository> logger)
    : IPersonSourceMachineRepository
{
    private readonly ISqlQueryExecutor _sqlExecutor = sqlExecutor;
    private readonly IMapPersonSourceMachineResponse _personSourceMachineResponseMapper = personSourceMachineResponseMapper;
    private readonly FluentLogger<PersonSourceMachineRepository> _logger = logger.Initializer();

    public async Task<PersonSourceMachine?> GetActiveAsync(int personId, int sourceMachineId)
    {
        try
        {
            return await _sqlExecutor.QuerySingleAsync
            (
                QueryPersonsSourceMachines.GetActiveSql,
                p =>
                {
                    p.AddWithValue(pn.PersonId, personId);
                    p.AddWithValue(pn.SourceMachineId, sourceMachineId);
                },
                reader => reader.ToPersonSourceMachine(_personSourceMachineResponseMapper)
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetActiveAsync failed for PersonId: [{PersonId}], SourceMachineId: [{SourceMachineId}]", personId, sourceMachineId);
            throw;
        }
    }

    public async Task<List<PersonSourceMachine>> ListActiveByPersonAsync(int personId)
    {
        try
        {
            return await _sqlExecutor.QueryManyAsync
            (
                QueryPersonsSourceMachines.ListActiveByPersonSql,
                p => p.AddWithValue(pn.PersonId, personId),
                reader => reader.ToPersonSourceMachine(_personSourceMachineResponseMapper)
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ListActiveByPersonAsync failed for PersonId: [{PersonId}]", personId);
            throw;
        }
    }

    public async Task<PersonSourceMachine?> CreateAsync(int personId, int sourceMachineId)
    {
        try
        {
            return await _sqlExecutor.QuerySingleAsync
            (
                QueryPersonsSourceMachines.AddSql,
                p =>
                {
                    p.AddWithValue(pn.PersonId, personId);
                    p.AddWithValue(pn.SourceMachineId, sourceMachineId);
                },
                reader => reader.ToPersonSourceMachine(_personSourceMachineResponseMapper)
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CreateAsync failed for PersonId: [{PersonId}], SourceMachineId: [{SourceMachineId}]", personId, sourceMachineId);
            throw;
        }
    }

    public async Task<PersonSourceMachine> UpsertAsync(int personId, int sourceMachineId)
    {
        try
        {
            var result = await _sqlExecutor.QuerySingleAsync
            (
                QueryPersonsSourceMachines.UpsertSql,
                p =>
                {
                    p.AddWithValue(pn.PersonId, personId);
                    p.AddWithValue(pn.SourceMachineId, sourceMachineId);
                    p.AddWithValue(pn.UpdatedOn, DateTimeOffset.UtcNow);
                },
                reader => reader.ToPersonSourceMachine(_personSourceMachineResponseMapper)
            );

            // UpsertSql's update/insert CTE pair always produces exactly one row between them.
            return result!;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UpsertAsync failed for PersonId: [{PersonId}], SourceMachineId: [{SourceMachineId}]", personId, sourceMachineId);
            throw;
        }
    }
}
