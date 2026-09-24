using Media.Common.Helpers.Fluent;
using Media.Database.Models;
using Media.Database.Repositories.Queries;
using Media.Database.Repositories.Queries.Helpers;
using Microsoft.Extensions.Logging;
#pragma warning disable CS8981
using pn = Media.Database.Repositories.Schemas.ParameterNames;
#pragma warning restore CS8981

namespace Media.Database.Repositories;

/// <inheritdoc cref="IPersonAvatarRepository"/>
public class PersonAvatarRepository(
    ICqlQueryExecutor cqlExecutor,
    ILogger<PersonAvatarRepository> logger) : IPersonAvatarRepository
{
    private readonly ICqlQueryExecutor _cqlExecutor = cqlExecutor;
    private readonly FluentLogger<PersonAvatarRepository> _logger = logger.Initializer();

    public async Task<PersonAvatar?> GetAsync(int personId)
    {
        try
        {
            return await _cqlExecutor.QuerySingleAsync(
                QueryPersonAvatars.GetSql,
                p => p.AddWithValue(pn.PersonId, personId),
                row => row.ToPersonAvatar());
        }
        catch (Exception ex)
        {
            _logger.WithCaller().LogError(ex, "GetAsync failed for a person avatar. PersonId: [{PersonId}]", personId);
            throw;
        }
    }

    public async Task SaveAsync(PersonAvatar avatar)
    {
        try
        {
            await _cqlExecutor.ExecuteAsync(
                QueryPersonAvatars.UpsertSql,
                p =>
                {
                    p.AddWithValue(pn.PersonId, avatar.PersonId);
                    p.AddWithValue(pn.ContentType, avatar.ContentType);
                    p.AddWithValue(pn.Image, avatar.Image);
                    p.AddWithValue(pn.UpdatedOn, avatar.UpdatedOn);
                });
        }
        catch (Exception ex)
        {
            _logger.WithCaller().LogError(ex, "SaveAsync failed for a person avatar. PersonId: [{PersonId}]", avatar.PersonId);
            throw;
        }
    }

    public async Task DeleteAsync(int personId)
    {
        try
        {
            await _cqlExecutor.ExecuteAsync(
                QueryPersonAvatars.DeleteSql,
                p => p.AddWithValue(pn.PersonId, personId));
        }
        catch (Exception ex)
        {
            _logger.WithCaller().LogError(ex, "DeleteAsync failed for a person avatar. PersonId: [{PersonId}]", personId);
            throw;
        }
    }
}
