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
}
