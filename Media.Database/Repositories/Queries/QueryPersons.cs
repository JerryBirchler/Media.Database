using Media.Database.Helpers;
using Media.Database.Mappers;
using Media.Database.Models;
using Npgsql;

#pragma warning disable CS8981
using cp = Media.Database.Repositories.Schemas.TablesSql.PersonsColumns;
using os = Media.Database.Repositories.Schemas.OrdinalsSql;
using pn = Media.Database.Repositories.Schemas.ParameterNames;
using ts = Media.Database.Repositories.Schemas.TablesSql;
#pragma warning restore CS8981

namespace Media.Database.Repositories.Queries;

/// <summary>
/// SQL query text, and reader/row mapping extensions, for people.
/// </summary>
public static class QueryPersons
{
    #region SQL Queries

    /// <summary>
    /// SQL to select a person matching the same contact-information tuple as
    /// <c>IX_Persons_ContactInformation</c> (first name, last name, email address, cell phone
    /// number) -- a lookup only, used to find-or-create rather than blindly inserting.
    /// </summary>
    public static string GetByContactInformationSql => $@"
        SELECT
            {cp.PersonId},
            {cp.PersonUuid},
            {cp.EmailAddress},
            {cp.CellPhoneNumber},
            {cp.FirstName},
            {cp.LastName},
            {cp.IsActive},
            {cp.InsertedOn},
            {cp.UpdatedOn}
        FROM {ts.Persons}
        WHERE
            {cp.FirstName} = {pn.FirstName}
            AND {cp.LastName} = {pn.LastName}
            AND {cp.EmailAddress} = {pn.EmailAddress}
            AND {cp.CellPhoneNumber} = {pn.CellPhoneNumber}
        ;";

    /// <summary>
    /// SQL to insert a new person, returning the inserted row.
    /// </summary>
    public static string AddPersonSql => $@"
        INSERT INTO {ts.Persons} (
            {cp.EmailAddress},
            {cp.CellPhoneNumber},
            {cp.FirstName},
            {cp.LastName}
        ) VALUES (
            {pn.EmailAddress},
            {pn.CellPhoneNumber},
            {pn.FirstName},
            {pn.LastName}
        )
        RETURNING
            {cp.PersonId},
            {cp.PersonUuid},
            {cp.EmailAddress},
            {cp.CellPhoneNumber},
            {cp.FirstName},
            {cp.LastName},
            {cp.IsActive},
            {cp.InsertedOn},
            {cp.UpdatedOn}
        ;";

    #endregion

    /// <summary>
    /// Maps the current row of <paramref name="reader"/> to a <see cref="Person"/>, via
    /// <paramref name="mapper"/> -- see <see cref="IMapPersonResponse"/> for why the actual
    /// construction is delegated rather than done inline here.
    /// </summary>
    public static Person ToPerson(this NpgsqlDataReader reader, IMapPersonResponse mapper)
    {
        return mapper.ToPerson(
            reader.GetInt32(os.PersonId),
            reader.GetGuid(os.PersonUuid),
            reader.GetString(os.EmailAddress),
            reader.GetString(os.CellPhoneNumber),
            reader.GetString(os.FirstName),
            reader.GetString(os.LastName),
            reader.GetFieldValue<bool>(os.IsActive),
            reader.GetFieldValue<DateTimeOffset>(os.InsertedOn),
            reader.GetFieldValue<DateTimeOffset?>(os.UpdatedOn));
    }
}
