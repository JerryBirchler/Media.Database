using Media.Common.Helpers.Fluent;
using Media.Database.Mappers;
using Media.Database.Models;
using Media.Database.Repositories.Queries;
using Microsoft.Extensions.Logging;

#pragma warning disable CS8981
using pn = Media.Database.Repositories.Schemas.ParameterNames;
#pragma warning restore CS8981

namespace Media.Database.Repositories;

/// <inheritdoc cref="IPersonRepository"/>
public class PersonRepository(
    ISqlQueryExecutor sqlExecutor,
    IMapPersonResponse personResponseMapper,
    ILogger<PersonRepository> logger)
    : IPersonRepository
{
    private readonly ISqlQueryExecutor _sqlExecutor = sqlExecutor;
    private readonly IMapPersonResponse _personResponseMapper = personResponseMapper;
    private readonly FluentLogger<PersonRepository> _logger = logger.Initializer();

    public async Task<Person?> FindOrCreateAsync(string firstName, string lastName, string emailAddress, string cellPhoneNumber)
    {
        try
        {
            var existing = await _sqlExecutor.QuerySingleAsync
            (
                QueryPersons.GetByContactInformationSql,
                p =>
                {
                    p.AddWithValue(pn.FirstName, firstName);
                    p.AddWithValue(pn.LastName, lastName);
                    p.AddWithValue(pn.EmailAddress, emailAddress);
                    p.AddWithValue(pn.CellPhoneNumber, cellPhoneNumber);
                },
                reader => reader.ToPerson(_personResponseMapper)
            );

            if (existing is not null)
                return existing;

            return await _sqlExecutor.QuerySingleAsync
            (
                QueryPersons.AddPersonSql,
                p =>
                {
                    p.AddWithValue(pn.EmailAddress, emailAddress);
                    p.AddWithValue(pn.CellPhoneNumber, cellPhoneNumber);
                    p.AddWithValue(pn.FirstName, firstName);
                    p.AddWithValue(pn.LastName, lastName);
                },
                reader => reader.ToPerson(_personResponseMapper)
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "FindOrCreateAsync failed for EmailAddress {EmailAddress}", emailAddress);
            throw;
        }
    }

    public async Task<Person?> GetByUuidAsync(Guid personUuid)
    {
        try
        {
            return await _sqlExecutor.QuerySingleAsync
            (
                QueryPersons.GetByUuidSql,
                p => p.AddWithValue(pn.PersonUuid, personUuid),
                reader => reader.ToPerson(_personResponseMapper)
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetByUuidAsync failed for PersonUuid {PersonUuid}", personUuid);
            throw;
        }
    }

    public async Task<Person?> CreateAsync(string firstName, string lastName, string emailAddress, string cellPhoneNumber, int createdByPersonId)
    {
        try
        {
            return await _sqlExecutor.QuerySingleAsync
            (
                QueryPersons.AddPersonWithCreatorSql,
                p =>
                {
                    p.AddWithValue(pn.EmailAddress, emailAddress);
                    p.AddWithValue(pn.CellPhoneNumber, cellPhoneNumber);
                    p.AddWithValue(pn.FirstName, firstName);
                    p.AddWithValue(pn.LastName, lastName);
                    p.AddWithValue(pn.CreatedByPersonId, createdByPersonId);
                },
                reader => reader.ToPerson(_personResponseMapper)
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CreateAsync failed for EmailAddress {EmailAddress}", emailAddress);
            throw;
        }
    }

    public async Task<List<Person>> ListByCreatorAsync(int createdByPersonId)
    {
        try
        {
            return await _sqlExecutor.QueryManyAsync
            (
                QueryPersons.ListByCreatorSql,
                p => p.AddWithValue(pn.CreatedByPersonId, createdByPersonId),
                reader => reader.ToPerson(_personResponseMapper)
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ListByCreatorAsync failed for CreatedByPersonId {CreatedByPersonId}", createdByPersonId);
            throw;
        }
    }

    public async Task<List<Person>> ListAllAsync()
    {
        try
        {
            return await _sqlExecutor.QueryManyAsync
            (
                QueryPersons.ListAllSql,
                p => { },
                reader => reader.ToPerson(_personResponseMapper)
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ListAllAsync failed");
            throw;
        }
    }

    public async Task<Person?> SetActiveAsync(int personId, bool isActive)
    {
        try
        {
            return await _sqlExecutor.QuerySingleAsync
            (
                QueryPersons.SetActiveSql,
                p =>
                {
                    p.AddWithValue(pn.PersonId, personId);
                    p.AddWithValue(pn.IsActive, isActive);
                    p.AddWithValue(pn.UpdatedOn, DateTimeOffset.UtcNow);
                },
                reader => reader.ToPerson(_personResponseMapper)
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SetActiveAsync failed for PersonId {PersonId}", personId);
            throw;
        }
    }

    public async Task<Person?> UpdateAsync(int personId, string firstName, string lastName, string emailAddress, string cellPhoneNumber, bool isActive, bool isEmailVerified, bool isSmsVerified)
    {
        try
        {
            return await _sqlExecutor.QuerySingleAsync
            (
                QueryPersons.UpdateSql,
                p =>
                {
                    p.AddWithValue(pn.PersonId, personId);
                    p.AddWithValue(pn.FirstName, firstName);
                    p.AddWithValue(pn.LastName, lastName);
                    p.AddWithValue(pn.EmailAddress, emailAddress);
                    p.AddWithValue(pn.CellPhoneNumber, cellPhoneNumber);
                    p.AddWithValue(pn.IsActive, isActive);
                    p.AddWithValue(pn.IsEmailVerified, isEmailVerified);
                    p.AddWithValue(pn.IsSmsVerified, isSmsVerified);
                    p.AddWithValue(pn.UpdatedOn, DateTimeOffset.UtcNow);
                },
                reader => reader.ToPerson(_personResponseMapper)
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UpdateAsync failed for PersonId {PersonId}", personId);
            throw;
        }
    }
}
