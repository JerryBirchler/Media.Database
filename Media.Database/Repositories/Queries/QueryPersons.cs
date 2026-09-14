using Cassandra;
using Media.Database.Helpers;
using Media.Database.Mappers;
using Media.Database.Models;
using Npgsql;

#pragma warning disable CS8981
using cp = Media.Database.Repositories.Schemas.TablesSql.PersonsColumns;
using ccp = Media.Database.Repositories.Schemas.TablesCql.PersonsColumns;
using os = Media.Database.Repositories.Schemas.OrdinalsSql;
using pn = Media.Database.Repositories.Schemas.ParameterNames;
using tc = Media.Database.Repositories.Schemas.TablesCql;
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
            {cp.CreatedByPersonId},
            {cp.IsSuperAdmin},
            {cp.IsEmailVerified},
            {cp.IsSmsVerified},
            {cp.OtpWindowOverrideMinutes},
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
    /// SQL to select a person by their <c>PersonId</c> -- the PostgreSQL-fallback path for
    /// <see cref="PersonRepository.GetByIdsAsync"/> when Scylla doesn't have the row yet.
    /// </summary>
    public static string GetByIdSql => $@"
        SELECT
            {cp.PersonId},
            {cp.PersonUuid},
            {cp.EmailAddress},
            {cp.CellPhoneNumber},
            {cp.FirstName},
            {cp.LastName},
            {cp.IsActive},
            {cp.CreatedByPersonId},
            {cp.IsSuperAdmin},
            {cp.IsEmailVerified},
            {cp.IsSmsVerified},
            {cp.OtpWindowOverrideMinutes},
            {cp.InsertedOn},
            {cp.UpdatedOn}
        FROM {ts.Persons}
        WHERE {cp.PersonId} = {pn.PersonId}
        ;";

    /// <summary>
    /// SQL to select a person by their <c>PersonUuid</c> -- the X-API-KEY lookup used by
    /// <c>PersonApiKeyAuthenticationHandler</c> to authenticate the Groups/Persons admin API.
    /// </summary>
    public static string GetByUuidSql => $@"
        SELECT
            {cp.PersonId},
            {cp.PersonUuid},
            {cp.EmailAddress},
            {cp.CellPhoneNumber},
            {cp.FirstName},
            {cp.LastName},
            {cp.IsActive},
            {cp.CreatedByPersonId},
            {cp.IsSuperAdmin},
            {cp.IsEmailVerified},
            {cp.IsSmsVerified},
            {cp.OtpWindowOverrideMinutes},
            {cp.InsertedOn},
            {cp.UpdatedOn}
        FROM {ts.Persons}
        WHERE {cp.PersonUuid} = {pn.PersonUuid}
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
            {cp.CreatedByPersonId},
            {cp.IsSuperAdmin},
            {cp.IsEmailVerified},
            {cp.IsSmsVerified},
            {cp.OtpWindowOverrideMinutes},
            {cp.InsertedOn},
            {cp.UpdatedOn}
        ;";

    /// <summary>
    /// SQL to insert a new person with an explicit <c>CreatedByPersonId</c> -- the
    /// <c>POST /api/persons</c> creation path (distinct from <see cref="AddPersonSql"/>, which the
    /// device-registration find-or-create flow uses and which has no creator concept).
    /// </summary>
    public static string AddPersonWithCreatorSql => $@"
        INSERT INTO {ts.Persons} (
            {cp.EmailAddress},
            {cp.CellPhoneNumber},
            {cp.FirstName},
            {cp.LastName},
            {cp.CreatedByPersonId}
        ) VALUES (
            {pn.EmailAddress},
            {pn.CellPhoneNumber},
            {pn.FirstName},
            {pn.LastName},
            {pn.CreatedByPersonId}
        )
        RETURNING
            {cp.PersonId},
            {cp.PersonUuid},
            {cp.EmailAddress},
            {cp.CellPhoneNumber},
            {cp.FirstName},
            {cp.LastName},
            {cp.IsActive},
            {cp.CreatedByPersonId},
            {cp.IsSuperAdmin},
            {cp.IsEmailVerified},
            {cp.IsSmsVerified},
            {cp.OtpWindowOverrideMinutes},
            {cp.InsertedOn},
            {cp.UpdatedOn}
        ;";

    /// <summary>
    /// SQL to select every person a given person created (active and inactive alike) -- the
    /// non-super-admin view of <c>GET /api/persons</c>.
    /// </summary>
    public static string ListByCreatorSql => $@"
        SELECT
            {cp.PersonId},
            {cp.PersonUuid},
            {cp.EmailAddress},
            {cp.CellPhoneNumber},
            {cp.FirstName},
            {cp.LastName},
            {cp.IsActive},
            {cp.CreatedByPersonId},
            {cp.IsSuperAdmin},
            {cp.IsEmailVerified},
            {cp.IsSmsVerified},
            {cp.OtpWindowOverrideMinutes},
            {cp.InsertedOn},
            {cp.UpdatedOn}
        FROM {ts.Persons}
        WHERE {cp.CreatedByPersonId} = {pn.CreatedByPersonId}
        ;";

    /// <summary>
    /// SQL to select every person -- the super-admin view of <c>GET /api/persons</c>.
    /// </summary>
    public static string ListAllSql => $@"
        SELECT
            {cp.PersonId},
            {cp.PersonUuid},
            {cp.EmailAddress},
            {cp.CellPhoneNumber},
            {cp.FirstName},
            {cp.LastName},
            {cp.IsActive},
            {cp.CreatedByPersonId},
            {cp.IsSuperAdmin},
            {cp.IsEmailVerified},
            {cp.IsSmsVerified},
            {cp.OtpWindowOverrideMinutes},
            {cp.InsertedOn},
            {cp.UpdatedOn}
        FROM {ts.Persons}
        ;";

    /// <summary>SQL to set a person's <c>IsActive</c> flag, returning the updated row.</summary>
    public static string SetActiveSql => $@"
        UPDATE {ts.Persons} SET
            {cp.IsActive} = {pn.IsActive},
            {cp.UpdatedOn} = {pn.UpdatedOn}
        WHERE {cp.PersonId} = {pn.PersonId}
        RETURNING
            {cp.PersonId},
            {cp.PersonUuid},
            {cp.EmailAddress},
            {cp.CellPhoneNumber},
            {cp.FirstName},
            {cp.LastName},
            {cp.IsActive},
            {cp.CreatedByPersonId},
            {cp.IsSuperAdmin},
            {cp.IsEmailVerified},
            {cp.IsSmsVerified},
            {cp.OtpWindowOverrideMinutes},
            {cp.InsertedOn},
            {cp.UpdatedOn}
        ;";

    /// <summary>
    /// SQL for the self-update path (<c>PUT /api/persons</c>): updates contact fields and,
    /// atomically, whatever active/verification flags the caller computed should follow from a
    /// changed email/cell phone number. Returns the updated row, or no rows if the target person
    /// does not exist.
    /// </summary>
    public static string UpdateSql => $@"
        UPDATE {ts.Persons} SET
            {cp.FirstName} = {pn.FirstName},
            {cp.LastName} = {pn.LastName},
            {cp.EmailAddress} = {pn.EmailAddress},
            {cp.CellPhoneNumber} = {pn.CellPhoneNumber},
            {cp.IsActive} = {pn.IsActive},
            {cp.IsEmailVerified} = {pn.IsEmailVerified},
            {cp.IsSmsVerified} = {pn.IsSmsVerified},
            {cp.UpdatedOn} = {pn.UpdatedOn}
        WHERE {cp.PersonId} = {pn.PersonId}
        RETURNING
            {cp.PersonId},
            {cp.PersonUuid},
            {cp.EmailAddress},
            {cp.CellPhoneNumber},
            {cp.FirstName},
            {cp.LastName},
            {cp.IsActive},
            {cp.CreatedByPersonId},
            {cp.IsSuperAdmin},
            {cp.IsEmailVerified},
            {cp.IsSmsVerified},
            {cp.OtpWindowOverrideMinutes},
            {cp.InsertedOn},
            {cp.UpdatedOn}
        ;";

    #endregion

    #region CQL Queries

    /// <summary>CQL to select a person by their <c>person_id</c> (Scylla-hydration lookup).</summary>
    public static string GetByIdCql => $@"
        SELECT
            {ccp.PersonId},
            {ccp.PersonUuid},
            {ccp.EmailAddress},
            {ccp.CellPhoneNumber},
            {ccp.FirstName},
            {ccp.LastName},
            {ccp.IsActive},
            {ccp.CreatedByPersonId},
            {ccp.IsSuperAdmin},
            {ccp.IsEmailVerified},
            {ccp.IsSmsVerified},
            {ccp.OtpWindowOverrideMinutes},
            {ccp.InsertedOn},
            {ccp.UpdatedOn}
        FROM
            {tc.Persons}
        WHERE
            {ccp.PersonId} = {pn.PersonId}
        LIMIT 1
        ;";

    /// <summary>CQL to insert/replace a person row.</summary>
    public static string UpsertCql => $@"
        INSERT INTO {tc.Persons}
        (
            {ccp.PersonId},
            {ccp.PersonUuid},
            {ccp.EmailAddress},
            {ccp.CellPhoneNumber},
            {ccp.FirstName},
            {ccp.LastName},
            {ccp.IsActive},
            {ccp.CreatedByPersonId},
            {ccp.IsSuperAdmin},
            {ccp.IsEmailVerified},
            {ccp.IsSmsVerified},
            {ccp.OtpWindowOverrideMinutes},
            {ccp.InsertedOn},
            {ccp.UpdatedOn}
        )
        VALUES
        (
            {pn.PersonId},
            {pn.PersonUuid},
            {pn.EmailAddress},
            {pn.CellPhoneNumber},
            {pn.FirstName},
            {pn.LastName},
            {pn.IsActive},
            {pn.CreatedByPersonId},
            {pn.IsSuperAdmin},
            {pn.IsEmailVerified},
            {pn.IsSmsVerified},
            {pn.OtpWindowOverrideMinutes},
            {pn.InsertedOn},
            {pn.UpdatedOn}
        )
        ;";

    /// <summary>CQL to delete a person row by <c>person_id</c>.</summary>
    public static string DeleteCql => $@"
        DELETE FROM {tc.Persons} WHERE {ccp.PersonId} = {pn.PersonId};";

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
            reader.GetFieldValue<int?>(os.CreatedByPersonId),
            reader.GetFieldValue<bool>(os.IsSuperAdmin),
            reader.GetFieldValue<bool>(os.IsEmailVerified),
            reader.GetFieldValue<bool>(os.IsSmsVerified),
            reader.GetFieldValue<int?>(os.OtpWindowOverrideMinutes),
            reader.GetFieldValue<DateTimeOffset>(os.InsertedOn),
            reader.GetFieldValue<DateTimeOffset?>(os.UpdatedOn));
    }

    /// <summary>Maps a Cassandra/Scylla <paramref name="row"/> to a <see cref="Person"/>.</summary>
    public static Person ToPerson(this Row row)
    {
        return new Person
        {
            PersonId = row.GetValue<int>(ccp.PersonId),
            PersonUuid = row.GetValue<Guid>(ccp.PersonUuid),
            EmailAddress = row.GetValue<string>(ccp.EmailAddress),
            CellPhoneNumber = row.GetValue<string>(ccp.CellPhoneNumber),
            FirstName = row.GetValue<string>(ccp.FirstName),
            LastName = row.GetValue<string>(ccp.LastName),
            IsActive = row.GetValue<bool>(ccp.IsActive),
            CreatedByPersonId = row.GetValue<int?>(ccp.CreatedByPersonId),
            IsSuperAdmin = row.GetValue<bool>(ccp.IsSuperAdmin),
            IsEmailVerified = row.GetValue<bool>(ccp.IsEmailVerified),
            IsSmsVerified = row.GetValue<bool>(ccp.IsSmsVerified),
            OtpWindowOverrideMinutes = row.GetValue<int?>(ccp.OtpWindowOverrideMinutes),
            InsertedOn = row.GetValue<DateTimeOffset>(ccp.InsertedOn),
            UpdatedOn = row.GetValue<DateTimeOffset?>(ccp.UpdatedOn)
        };
    }
}
