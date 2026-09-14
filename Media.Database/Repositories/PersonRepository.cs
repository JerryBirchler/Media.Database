using Media.Common.Helpers.Fluent;
using Media.Common.Providers;
using Media.Database.Mappers;
using Media.Database.Models;
using Media.Database.Repositories.Queries;
using Media.Database.Repositories.Queries.Helpers;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

#pragma warning disable CS8981
using pn = Media.Database.Repositories.Schemas.ParameterNames;
#pragma warning restore CS8981

namespace Media.Database.Repositories;

/// <summary>
/// PostgreSQL-backed implementation of <see cref="IPersonRepository"/>. Writes go to PostgreSQL
/// only -- Scylla is kept in sync separately and asynchronously by the CDC pipeline (see
/// Cdc.PersonsCdcSyncHandler), same split as <see cref="FileRepository"/>. <see cref="GetByIdsAsync"/>
/// hydrates preferring Scylla, falling back to PostgreSQL per row.
/// </summary>
public class PersonRepository(
    ISqlQueryExecutor sqlExecutor,
    ICqlQueryExecutor cqlExecutor,
    IScyllaSessionProvider scyllaProvider,
    IMapPersonResponse personResponseMapper,
    ILogger<PersonRepository> logger)
    : BaseRepository(scyllaProvider), IPersonRepository
{
    private readonly ISqlQueryExecutor _sqlExecutor = sqlExecutor;
    private readonly ICqlQueryExecutor _cqlExecutor = cqlExecutor;
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

    public async Task<List<PersonIdentifier>> GetPersonIdentifiersByCreatorIdAsync(int createdByPersonId, PersonIdentifier? next, int limit)
    {
        try
        {
            var afterLastName = next?.LastName;
            var afterFirstName = next?.FirstName;
            var afterPersonUuid = next?.PersonUuid;

            return await _sqlExecutor.QueryManyAsync
            (
                QueryPersons.GetPersonIdentifiersByCreatorSql,
                p =>
                {
                    p.AddWithValue(pn.CreatedByPersonId, createdByPersonId);
                    p.AddWithValue(pn.LastName, afterLastName.ToNullableValueForSql());
                    p.AddWithValue(pn.FirstName, afterFirstName.ToNullableValueForSql());
                    p.AddWithValue(pn.PersonUuid, afterPersonUuid.ToNullableValueForSql());
                    p.AddWithValue(pn.Limit, limit);
                },
                reader => reader.ToPersonIdentifier()
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetPersonIdentifiersByCreatorIdAsync failed for CreatedByPersonId {CreatedByPersonId}", createdByPersonId);
            throw;
        }
    }

    public async Task<List<PersonIdentifier>> GetAllPersonIdentifiersAsync(PersonIdentifier? next, int limit)
    {
        try
        {
            var afterLastName = next?.LastName;
            var afterFirstName = next?.FirstName;
            var afterPersonUuid = next?.PersonUuid;

            return await _sqlExecutor.QueryManyAsync
            (
                QueryPersons.GetAllPersonIdentifiersSql,
                p =>
                {
                    p.AddWithValue(pn.LastName, afterLastName.ToNullableValueForSql());
                    p.AddWithValue(pn.FirstName, afterFirstName.ToNullableValueForSql());
                    p.AddWithValue(pn.PersonUuid, afterPersonUuid.ToNullableValueForSql());
                    p.AddWithValue(pn.Limit, limit);
                },
                reader => reader.ToPersonIdentifier()
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetAllPersonIdentifiersAsync failed");
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

    public async Task<List<Person>> GetByIdsAsync(IEnumerable<int> personIds, int maxDegreeOfParallelism)
    {
        var results = new ConcurrentBag<Person>();

        await Parallel.ForEachAsync(
            personIds,
            new ParallelOptions { MaxDegreeOfParallelism = maxDegreeOfParallelism },
            async (personId, _) =>
            {
                var person = await GetByIdPreferringScyllaAsync(personId);
                if (person is not null)
                    results.Add(person);
            });

        return [.. results];
    }

    /// <summary>
    /// Looks up a single person by id, preferring Scylla and falling back to PostgreSQL when
    /// Scylla has no row yet (CDC lag) or is unreachable. A Scylla failure here is deliberately
    /// not rethrown -- PostgreSQL is the durable source of truth, so a temporarily unavailable
    /// Scylla cluster should degrade this to a normal PostgreSQL read rather than fail it.
    /// </summary>
    private async Task<Person?> GetByIdPreferringScyllaAsync(int personId)
    {
        var log = _logger.WithCaller();

        try
        {
            var fromScylla = await _cqlExecutor.QuerySingleAsync(
                QueryPersons.GetByIdCql,
                p => p.AddWithValue(pn.PersonId, personId),
                row => row.ToPerson());

            if (fromScylla is not null)
                return fromScylla;
        }
        catch (Exception ex) when (IsScyllaConnectivityException(ex))
        {
            log.LogError(ex, "Scylla cluster unavailable fetching PersonId {PersonId}; falling back to Postgres", personId);
            await TryHealScyllaSessionAsync(_logger, nameof(GetByIdPreferringScyllaAsync));
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Scylla lookup failed for PersonId {PersonId}; falling back to Postgres", personId);
        }

        return await _sqlExecutor.QuerySingleAsync
        (
            QueryPersons.GetByIdSql,
            p => p.AddWithValue(pn.PersonId, personId),
            reader => reader.ToPerson(_personResponseMapper)
        );
    }
}
