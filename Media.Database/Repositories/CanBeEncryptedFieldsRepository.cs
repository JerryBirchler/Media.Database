using Media.Common.Helpers.Fluent;
using Media.Database.Models;
using Media.Database.Repositories.Queries;
using Media.Database.Repositories.Queries.Helpers;
using Microsoft.Extensions.Logging;

#pragma warning disable CS8981
using pn = Media.Database.Repositories.Schemas.ParameterNames;
#pragma warning restore CS8981

namespace Media.Database.Repositories;

/// <inheritdoc cref="ICanBeEncryptedFieldsRepository"/>
public class CanBeEncryptedFieldsRepository(
    ICqlQueryExecutor cqlExecutor,
    ILogger<CanBeEncryptedFieldsRepository> logger) : ICanBeEncryptedFieldsRepository
{
    private readonly ICqlQueryExecutor _cqlExecutor = cqlExecutor;
    private readonly FluentLogger<CanBeEncryptedFieldsRepository> _logger = logger.Initializer();

    public async Task<List<CanBeEncryptedField>> GetAllAsync()
    {
        try
        {
            return await _cqlExecutor.QueryManyAsync(
                QueryCanBeEncryptedFields.GetAllSql,
                p => { },
                row => row.ToCanBeEncryptedField());
        }
        catch (Exception ex)
        {
            _logger.WithCaller().LogError(ex, "GetAllAsync failed");
            throw;
        }
    }

    public async Task RegisterAsync(string typeName, string memberName, int releaseIntroduced)
    {
        try
        {
            await _cqlExecutor.ExecuteAsync(
                QueryCanBeEncryptedFields.RegisterSql,
                p =>
                {
                    p.AddWithValue(pn.TableName, typeName);
                    p.AddWithValue(pn.ColumnName, memberName);
                    p.AddWithValue(pn.ReleaseIntroduced, releaseIntroduced);
                    p.AddWithValue(pn.InsertedOn, DateTimeOffset.UtcNow);
                });
        }
        catch (Exception ex)
        {
            _logger.WithCaller().LogError(ex, "RegisterAsync failed for TypeName {TypeName}, MemberName {MemberName}", typeName, memberName);
            throw;
        }
    }

    public async Task SetReleaseRemovedAsync(string typeName, string memberName, int releaseRemoved)
    {
        try
        {
            await _cqlExecutor.ExecuteAsync(
                QueryCanBeEncryptedFields.SetReleaseRemovedSql,
                p =>
                {
                    p.AddWithValue(pn.TableName, typeName);
                    p.AddWithValue(pn.ColumnName, memberName);
                    p.AddWithValue(pn.ReleaseRemoved, releaseRemoved);
                    p.AddWithValue(pn.UpdatedOn, DateTimeOffset.UtcNow);
                });
        }
        catch (Exception ex)
        {
            _logger.WithCaller().LogError(ex, "SetReleaseRemovedAsync failed for TypeName {TypeName}, MemberName {MemberName}", typeName, memberName);
            throw;
        }
    }
}
