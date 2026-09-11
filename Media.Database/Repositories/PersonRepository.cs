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
}
