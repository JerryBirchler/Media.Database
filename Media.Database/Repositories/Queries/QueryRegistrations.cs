using Cassandra;
using Media.Database.Helpers;
using Media.Database.Models;
using Npgsql;

#pragma warning disable CS8981
using ccr = Media.Database.Repositories.Schemas.TablesCql.RegistrationsColumns;
using cp = Media.Database.Repositories.Schemas.TablesSql.PersonsColumns;
using cpsm = Media.Database.Repositories.Schemas.TablesSql.PersonsSourceMachinesColumns;
using csr = Media.Database.Repositories.Schemas.TablesSql.RegistrationsColumns;
using cssmr = Media.Database.Repositories.Schemas.TablesSql.SourceMachineRegistrationsColumns;
using os = Media.Database.Repositories.Schemas.OrdinalsSql;
using pn = Media.Database.Repositories.Schemas.ParameterNames;
using tc = Media.Database.Repositories.Schemas.TablesCql;
using ts = Media.Database.Repositories.Schemas.TablesSql;
#pragma warning restore CS8981

namespace Media.Database.Repositories.Queries;

/// <summary>
/// SQL and CQL query text, and reader/row mapping extensions, for registration.
/// </summary>
public static class QueryRegistrations
{
    #region SQL Queries
    /// <summary>
    /// SQL to insert a new SourceMachine registration, returning the inserted row.
    /// </summary>
    public static string AddBySourceInformationSql => $@"
        INSERT INTO {ts.SourceMachineRegistrations} (
            {cssmr.SourceMachineName},
            {cssmr.DeviceTypeId},
            {cssmr.DisambiguationKey},
            {cssmr.EmailAddress},
            {cssmr.CellPhoneNumber},
            {cssmr.FirstName},
            {cssmr.LastName},
            {cssmr.OperatingSystem}
        ) VALUES (
            {pn.SourceMachineName},
            {pn.DeviceTypeId},
            {pn.DisambiguationKey},
            {pn.EmailAddress},
            {pn.CellPhoneNumber},
            {pn.FirstName},
            {pn.LastName},
            {pn.OperatingSystem}
        )
        RETURNING
            {cssmr.SourceMachineId},
            {cssmr.SourceMachineUuid},
            {cssmr.SourceMachineName},
            {cssmr.DeviceTypeId},
            {cssmr.DisambiguationKey},
            {cssmr.EmailAddress},
            {cssmr.CellPhoneNumber},
            {cssmr.FirstName},
            {cssmr.LastName},
            {cssmr.OperatingSystem},
            {cssmr.InsertedOn},
            {cssmr.IsActive}
        ;";

    /// <summary>
    /// SQL to update a SourceMachine registration, returning the updated row.
    /// </summary>
    public static string UpdateSourceInformationSql => $@"
        UPDATE {ts.SourceMachineRegistrations} SET
            {cssmr.EmailAddress} = {pn.EmailAddress},
            {cssmr.CellPhoneNumber} = {pn.CellPhoneNumber},
            {cssmr.OperatingSystem} = {pn.OperatingSystem}
        WHERE
            {cssmr.SourceMachineUuid} = {pn.SourceMachineUuid}
        RETURNING
            {cssmr.SourceMachineUuid},
            {cssmr.SourceMachineName},
            {cssmr.DeviceTypeId},
            {cssmr.EmailAddress},
            {cssmr.CellPhoneNumber},
            {cssmr.FirstName},
            {cssmr.LastName},
            {cssmr.OperatingSystem},
            {cssmr.InsertedOn},
            {cssmr.IsActive}
        ;";

    /// <summary>
    /// SQL to select a SourceMachine by the same identity tuple as
    /// <c>IX_SourcemachineRegistrations_SourceInformation</c> -- SourceMachineName, DeviceTypeId,
    /// FirstName, LastName. Email/cell phone are deliberately NOT part of the match: they're
    /// mutable contact info on an existing device (that unique index only covers the four identity
    /// columns; email/phone are index-only INCLUDE columns, not part of uniqueness), not part of
    /// what makes two registrations "the same device". Matching on all six here (as this used to)
    /// let a device re-registering with a changed email/phone slip past this lookup as "not found",
    /// then fail the INSERT below with a duplicate-key violation on that same index instead of
    /// updating the existing row -- see <see cref="RegistrationRepository.AddBySourceInformation"/>.
    /// </summary>
    public static string GetBySourceInformationSql => $@"
        SELECT
            r.{csr.Id},
            smr.{cssmr.SourceMachineId},
            smr.{cssmr.SourceMachineUuid},
            smr.{cssmr.SourceMachineName},
            smr.{cssmr.DeviceTypeId},
            smr.{cssmr.DisambiguationKey},
            smr.{cssmr.EmailAddress},
            smr.{cssmr.CellPhoneNumber},
            smr.{cssmr.FirstName},
            smr.{cssmr.LastName},
            CASE WHEN r.{csr.Id} IS NULL THEN False ELSE True END AS ""HasRegistration"",
            COALESCE(r.{csr.IsEmailVerified}, False) AS ""IsEmailVerified"",
            COALESCE(r.{csr.IsSmsVerified}, False) AS ""IsSmsVerified"",
            smr.{cssmr.OperatingSystem},
            smr.{cssmr.IsActive},
            smr.{cssmr.OwningPersonId},
            smr.{cssmr.GroupShellId},
            smr.{cssmr.InsertedOn},
            smr.{cssmr.UpdatedOn},
            r.{csr.OtpEmail},
            r.{csr.OtpCellPhone},
            r.{csr.InsertedOn} As ""RegistrationInsertedOn"",
            r.{csr.UpdatedOn} AS ""RegistrationUpdatedOn""
        FROM
            {ts.SourceMachineRegistrations} AS smr
        LEFT JOIN
            {ts.Registrations} AS r
        ON
            r.{csr.SourceMachineId} = smr.{cssmr.SourceMachineId}
            AND r.{csr.IsCurrent} = True
            AND smr.{cssmr.EmailAddress} = r.{csr.EmailAddress}
            AND smr.{cssmr.CellPhoneNumber} = r.{csr.CellPhoneNumber}
        WHERE
            smr.{cssmr.SourceMachineName} = {pn.SourceMachineName}
            AND smr.{cssmr.DeviceTypeId} = {pn.DeviceTypeId}
            AND smr.{cssmr.FirstName} = {pn.FirstName}
            AND smr.{cssmr.LastName} = {pn.LastName}
        LIMIT 1
        ;";


    /// <summary>SQL to select a SourceMachine by its unique identifier.</summary>
    public static string GetBySourceMachineUuidSql => $@"
        SELECT
            r.{csr.Id},
            smr.{cssmr.SourceMachineId},
            smr.{cssmr.SourceMachineUuid},
            smr.{cssmr.SourceMachineName},
            smr.{cssmr.DeviceTypeId},
            smr.{cssmr.DisambiguationKey},
            smr.{cssmr.EmailAddress},
            smr.{cssmr.CellPhoneNumber},
            smr.{cssmr.FirstName},
            smr.{cssmr.LastName},
            CASE WHEN r.{csr.Id} IS NULL THEN False ELSE True END AS ""HasRegistration"",
            COALESCE(r.{csr.IsEmailVerified}, False) AS ""IsEmailVerified"",
            COALESCE(r.{csr.IsSmsVerified}, False) AS ""IsSmsVerified"",
            smr.{cssmr.OperatingSystem},
            smr.{cssmr.IsActive},
            smr.{cssmr.OwningPersonId},
            smr.{cssmr.GroupShellId},
            smr.{cssmr.InsertedOn},
            smr.{cssmr.UpdatedOn},
            r.{csr.OtpEmail},
            r.{csr.OtpCellPhone},
            r.{csr.InsertedOn} As ""RegistrationInsertedOn"",
            r.{csr.UpdatedOn} AS ""RegistrationUpdatedOn""
        FROM
            {ts.SourceMachineRegistrations} AS smr
        LEFT JOIN
            {ts.Registrations} AS r
        ON
            r.{csr.SourceMachineId} = smr.{cssmr.SourceMachineId}
            AND r.{csr.IsCurrent} = True
            AND smr.{cssmr.EmailAddress} = r.{csr.EmailAddress}
            AND smr.{cssmr.CellPhoneNumber} = r.{csr.CellPhoneNumber}
        WHERE
            smr.{cssmr.SourceMachineUuid} = {pn.SourceMachineUuid}
        LIMIT 1
        ;";

    /// <summary>
    /// Gets the joined source-machine/current-registration state by source machine id -- the same
    /// shape as <see cref="GetBySourceMachineUuidSql"/>, but keyed by id since that's what a CDC
    /// change event for either the Registrations or SourceMachineRegistrations table carries.
    /// </summary>
    public static string GetBySourceMachineIdSql => $@"
        SELECT
            r.{csr.Id},
            smr.{cssmr.SourceMachineId},
            smr.{cssmr.SourceMachineUuid},
            smr.{cssmr.SourceMachineName},
            smr.{cssmr.DeviceTypeId},
            smr.{cssmr.DisambiguationKey},
            smr.{cssmr.EmailAddress},
            smr.{cssmr.CellPhoneNumber},
            smr.{cssmr.FirstName},
            smr.{cssmr.LastName},
            CASE WHEN r.{csr.Id} IS NULL THEN False ELSE True END AS ""HasRegistration"",
            COALESCE(r.{csr.IsEmailVerified}, False) AS ""IsEmailVerified"",
            COALESCE(r.{csr.IsSmsVerified}, False) AS ""IsSmsVerified"",
            smr.{cssmr.OperatingSystem},
            smr.{cssmr.IsActive},
            smr.{cssmr.OwningPersonId},
            smr.{cssmr.GroupShellId},
            smr.{cssmr.InsertedOn},
            smr.{cssmr.UpdatedOn},
            r.{csr.OtpEmail},
            r.{csr.OtpCellPhone},
            r.{csr.InsertedOn} As ""RegistrationInsertedOn"",
            r.{csr.UpdatedOn} AS ""RegistrationUpdatedOn""
        FROM
            {ts.SourceMachineRegistrations} AS smr
        LEFT JOIN
            {ts.Registrations} AS r
        ON
            r.{csr.SourceMachineId} = smr.{cssmr.SourceMachineId}
            AND r.{csr.IsCurrent} = True
            AND smr.{cssmr.EmailAddress} = r.{csr.EmailAddress}
            AND smr.{cssmr.CellPhoneNumber} = r.{csr.CellPhoneNumber}
        WHERE
            smr.{cssmr.SourceMachineId} = {pn.SourceMachineId}
        LIMIT 1
        ;";

    /// <summary>
    /// Resolves a <c>PersonSourceMachineUuid</c> (one person's association with one device) to
    /// that device's joined source-machine/registration state -- the multi-origin X-API-KEY model's
    /// second credential type (MEDIA-34). Same shape as <see cref="GetBySourceMachineUuidSql"/>,
    /// except IsEmailVerified/IsSmsVerified come from the *person's own* verification
    /// (Persons.IsEmailVerified/IsSmsVerified), not the device's -- a caller authenticating via
    /// this credential is acting as themselves, possibly on a device someone else registered, so
    /// their own verification state is what should gate access, not whichever registration history
    /// the device happens to carry. Requires PersonsSourceMachines/Persons/SourceMachineRegistrations
    /// to all be active, per the finalized "who has access to a device" rule (2026-09-12).
    /// </summary>
    public static string GetByPersonSourceMachineUuidSql => $@"
        SELECT
            smr.{cssmr.SourceMachineId},
            smr.{cssmr.SourceMachineUuid},
            smr.{cssmr.SourceMachineName},
            smr.{cssmr.DeviceTypeId},
            smr.{cssmr.DisambiguationKey},
            smr.{cssmr.EmailAddress},
            smr.{cssmr.CellPhoneNumber},
            smr.{cssmr.FirstName},
            smr.{cssmr.LastName},
            p.{cp.IsEmailVerified},
            p.{cp.IsSmsVerified},
            smr.{cssmr.OperatingSystem},
            smr.{cssmr.IsActive},
            smr.{cssmr.OwningPersonId},
            smr.{cssmr.GroupShellId},
            smr.{cssmr.InsertedOn},
            smr.{cssmr.UpdatedOn}
        FROM
            {ts.PersonsSourceMachines} AS psm
        JOIN
            {ts.Persons} AS p
        ON
            p.{cp.PersonId} = psm.{cpsm.PersonId}
            AND p.{cp.IsActive} = True
        JOIN
            {ts.SourceMachineRegistrations} AS smr
        ON
            smr.{cssmr.SourceMachineId} = psm.{cpsm.SourceMachineId}
            AND smr.{cssmr.IsActive} = True
        WHERE
            psm.{cpsm.PersonSourceMachineUuid} = {pn.PersonSourceMachineUuid}
            AND psm.{cpsm.IsActive} = True
        LIMIT 1
        ;";

    /// <summary>
    /// Inactivate current registrations by source machine UUID,
    /// and return the updated registration Ids.
    /// </summary>
    public static string InactivateRegistrationsBySourceMachineUuidSql => $@"
        UPDATE {ts.Registrations} AS r SET
            {csr.IsCurrent} = False,
            {csr.UpdatedOn} = {pn.UpdatedOn}
        FROM
            {ts.SourceMachineRegistrations} AS smr
        WHERE
            r.{csr.SourceMachineId} = smr.{cssmr.SourceMachineId}
            AND smr.{cssmr.SourceMachineUuid} = {pn.SourceMachineUuid}
            AND r.{csr.IsCurrent} = True
        RETURNING
            r.{csr.Id}
        ;";

    /// <summary>
    /// Add a new registration derived from the source machine by UUID.
    /// </summary>
    public static string AddRegistrationBySourceMachineUuidSql => $@"
        INSERT INTO {ts.Registrations} (
            {csr.SourceMachineId},
            {csr.EmailAddress},
            {csr.OtpEmail},
            {csr.IsEmailVerified},
            {csr.CellPhoneNumber},
            {csr.OtpCellPhone},
            {csr.IsSmsVerified},
            {csr.IsCurrent}
        )
        SELECT
            {cssmr.SourceMachineId},
            {cssmr.EmailAddress},
            {pn.OtpEmail},
            CASE WHEN {pn.OtpEmail} = '' THEN True ELSE False END,
            {cssmr.CellPhoneNumber},
            {pn.OtpCellPhone},
            CASE WHEN {pn.OtpCellPhone} = '' THEN True ELSE False END,
            True
        FROM
            {ts.SourceMachineRegistrations}
        WHERE
            {cssmr.SourceMachineUuid} = {pn.SourceMachineUuid}
        RETURNING
            {csr.Id},
            {csr.IsEmailVerified},
            {csr.IsSmsVerified},
            {csr.OtpEmail},
            {csr.OtpCellPhone},
            {csr.InsertedOn},
            {csr.UpdatedOn}
        ;";

    /// <summary>
    /// Update the registration when the one-time password for email is verified. The OTP is only
    /// honored within one hour of the pending registration row's <see cref="TablesSql.RegistrationsColumns.InsertedOn"/>;
    /// past that window this matches no row, the same as an incorrect code.
    /// </summary>
    public static string VerifyOtpEmailSql => $@"
        UPDATE {ts.Registrations} AS r SET
            {csr.IsEmailVerified} = True,
            {csr.UpdatedOn} = {pn.UpdatedOn}
        FROM
            {ts.SourceMachineRegistrations} AS smr
        WHERE
            r.{csr.SourceMachineId} = smr.{cssmr.SourceMachineId}
            AND smr.{cssmr.EmailAddress} = {pn.EmailAddress}
            AND smr.{cssmr.SourceMachineName} = {pn.SourceMachineName}
            AND smr.{cssmr.DeviceTypeId} = {pn.DeviceTypeId}
            AND r.{csr.IsCurrent} = True
            AND r.{csr.EmailAddress} = smr.{cssmr.EmailAddress}
            AND r.{csr.CellPhoneNumber} = smr.{cssmr.CellPhoneNumber}
            AND r.{csr.OtpEmail} = {pn.OtpEmail}
            AND r.{csr.InsertedOn} > {pn.OtpWindowStart}
        RETURNING
            smr.{cssmr.SourceMachineUuid},
            smr.{cssmr.SourceMachineName},
            smr.{cssmr.DeviceTypeId},
            smr.{cssmr.DisambiguationKey},
            smr.{cssmr.FirstName},
            smr.{cssmr.LastName},
            r.{csr.EmailAddress},
            smr.{cssmr.CellPhoneNumber},
            r.{csr.IsEmailVerified},
            r.{csr.IsSmsVerified}
        ;";

    /// <summary>
    /// Update the registration when the one-time password for cell phone is verified. The OTP is
    /// only honored within one hour of the pending registration row's <see cref="TablesSql.RegistrationsColumns.InsertedOn"/>;
    /// past that window this matches no row, the same as an incorrect code.
    /// </summary>
    public static string VerifyOtpCellPhoneSql => $@"
        UPDATE {ts.Registrations} AS r SET
            {csr.IsSmsVerified} = True,
            {csr.UpdatedOn} = {pn.UpdatedOn}
        FROM
            {ts.SourceMachineRegistrations} AS smr
        WHERE
            r.{csr.SourceMachineId} = smr.{cssmr.SourceMachineId}
            AND smr.{cssmr.CellPhoneNumber} = {pn.CellPhoneNumber}
            AND smr.{cssmr.SourceMachineName} = {pn.SourceMachineName}
            AND smr.{cssmr.DeviceTypeId} = {pn.DeviceTypeId}
            AND r.{csr.IsCurrent} = True
            AND r.{csr.EmailAddress} = smr.{cssmr.EmailAddress}
            AND r.{csr.CellPhoneNumber} = smr.{cssmr.CellPhoneNumber}
            AND r.{csr.OtpCellPhone} = {pn.OtpCellPhone}
            AND r.{csr.InsertedOn} > {pn.OtpWindowStart}
        RETURNING
            smr.{cssmr.SourceMachineUuid},
            smr.{cssmr.SourceMachineName},
            smr.{cssmr.DeviceTypeId},
            smr.{cssmr.DisambiguationKey},
            smr.{cssmr.FirstName},
            smr.{cssmr.LastName},
            r.{csr.CellPhoneNumber},
            r.{csr.IsSmsVerified},
            r.{csr.IsEmailVerified}
        ;";

    /// <summary>
    /// SQL to permanently set <c>SourceMachineRegistrations.OwningPersonId</c> to the device's
    /// registrant (MEDIA-10). A no-op once already set -- the <c>WHERE ... IS NULL</c> guard is
    /// what makes ownership permanent, since it can never be reassigned or cleared by any endpoint,
    /// through any path, including this one on a later registration of the same device.
    /// </summary>
    public static string SetOwningPersonIfUnsetSql => $@"
        UPDATE {ts.SourceMachineRegistrations} SET
            {cssmr.OwningPersonId} = {pn.OwningPersonId}
        WHERE
            {cssmr.SourceMachineId} = {pn.SourceMachineId}
            AND {cssmr.OwningPersonId} IS NULL
        ;";

    /// <summary>
    /// SQL to permanently set <c>SourceMachineRegistrations.GroupShellId</c> (MEDIA-37) -- the
    /// same "WHERE ... IS NULL" pattern as <see cref="SetOwningPersonIfUnsetSql"/>, so a device's
    /// shell assignment is a one-time thing this query can make, never a reassignment. Promotion
    /// or a later shell/group change (not yet built) would need its own, deliberate query.
    /// </summary>
    /// <summary>
    /// SQL to set the channel a customer chose to receive their generated group encryption key on.
    /// Deliberately NOT a "WHERE ... IS NULL" one-time set like
    /// <see cref="SetGroupShellIdIfUnsetSql"/>: a delivery preference is a preference, and the
    /// customer is allowed to change it.
    /// </summary>
    public static string SetKeyDeliveryMethodSql => $@"
        UPDATE {ts.SourceMachineRegistrations} SET
            {cssmr.KeyDeliveryMethod} = {pn.KeyDeliveryMethod}
        WHERE
            {cssmr.SourceMachineId} = {pn.SourceMachineId}
        ;";

    public static string SetGroupShellIdIfUnsetSql => $@"
        UPDATE {ts.SourceMachineRegistrations} SET
            {cssmr.GroupShellId} = {pn.GroupShellId}
        WHERE
            {cssmr.SourceMachineId} = {pn.SourceMachineId}
            AND {cssmr.GroupShellId} IS NULL
        ;";


    #endregion

    #region CQL Queries
    /// <summary>
    /// CQL to select a registration by <c>source_machine_id</c> -- the Scylla-hydration lookup for
    /// <see cref="RegistrationRepository.GetByIdsAsync"/>, reading the same "registrations" table
    /// <see cref="UpsertRegistrationCql"/> (via RegistrationsCdcSyncHandler) already maintains.
    /// </summary>
    public static string GetBySourceMachineIdCql => $@"
        SELECT
            {ccr.RegistrationId},
            {ccr.SourceMachineId},
            {ccr.SourceMachineUuid},
            {ccr.SourceMachineName},
            {ccr.DeviceTypeId},
            {ccr.DisambiguationKey},
            {ccr.FirstName},
            {ccr.LastName},
            {ccr.EmailAddress},
            {ccr.CellPhoneNumber},
            {ccr.OperatingSystem},
            {ccr.SourceInsertedOn},
            {ccr.SourceUpdatedOn},
            {ccr.IsActive},
            {ccr.IsEmailVerified},
            {ccr.IsSmsVerified},
            {ccr.OtpEmail},
            {ccr.OtpCellPhone},
            {ccr.RegistrationInsertedOn},
            {ccr.RegistrationUpdatedOn}
        FROM
            {tc.Registrations}
        WHERE
            {ccr.SourceMachineId} = {pn.SourceMachineId}
        LIMIT 1
        ;";

    public static string UpsertRegistrationCql => $@"
        INSERT INTO {tc.Registrations} (
            {ccr.SourceMachineUuid},
            {ccr.RegistrationId},
            {ccr.SourceMachineId},
            {ccr.SourceMachineName},
            {ccr.DeviceTypeId},
            {ccr.DisambiguationKey},
            {ccr.FirstName},
            {ccr.LastName},
            {ccr.OperatingSystem},
            {ccr.EmailAddress},
            {ccr.CellPhoneNumber},
            {ccr.SourceInsertedOn},
            {ccr.SourceUpdatedOn},
            {ccr.IsActive},
            {ccr.IsEmailVerified},
            {ccr.IsSmsVerified},
            {ccr.OtpEmail},
            {ccr.OtpCellPhone},
            {ccr.RegistrationInsertedOn},
            {ccr.RegistrationUpdatedOn}
        ) VALUES (
            {pn.SourceMachineUuid},
            {pn.RegistrationId},
            {pn.SourceMachineId},
            {pn.SourceMachineName},
            {pn.DeviceTypeId},
            {pn.DisambiguationKey},
            {pn.FirstName},
            {pn.LastName},
            {pn.OperatingSystem},
            {pn.EmailAddress},
            {pn.CellPhoneNumber},
            {pn.SourceInsertedOn},
            {pn.SourceUpdatedOn},
            {pn.IsActive},
            {pn.IsEmailVerified},
            {pn.IsSmsVerified},
            {pn.OtpEmail},
            {pn.OtpCellPhone},
            {pn.RegistrationInsertedOn},
            {pn.RegistrationUpdatedOn}
        );";
    #endregion

    /// <summary>
    /// Maps the current row of <paramref name="reader"/> to a <see cref="SourceMachineRegistrations"/>.
    /// The Registrations side of the LEFT JOIN this reads from (Id, OtpEmail, OtpCellPhone,
    /// RegistrationInsertedOn, RegistrationUpdatedOn) is null whenever a SourceMachineRegistrations
    /// row exists with no current Registrations row yet -- HasRegistration (itself a CASE
    /// expression, never null) is read first and used to guard those columns rather than reading
    /// them unconditionally, which would throw on the all-null row a LEFT JOIN produces.
    /// </summary>
    public static SourceMachineRegistrations ToSourceMachineRegistration(this NpgsqlDataReader reader)
    {
        var hasRegistration = reader.GetFieldValue<bool>(os.HasRegistration);

        return new SourceMachineRegistrations
        {
            RegistrationId = hasRegistration ? reader.GetInt32(os.Id) : 0,
            SourceMachineId = reader.GetInt32(os.SourceMachineId),
            SourceMachineUuid = reader.GetGuid(os.SourceMachineUuid),
            SourceMachineName = reader.GetString(os.SourceMachineName),
            DeviceTypeId = (DeviceTypes)reader.GetInt32(os.DeviceTypeId),
            DisambiguationKey = reader.GetString(os.DisambiguationKey),
            EmailAddress = reader.GetString(os.EmailAddress),
            CellPhoneNumber = reader.GetString(os.CellPhoneNumber)!,
            FirstName = reader.GetString(os.FirstName),
            LastName = reader.GetString(os.LastName),
            HasRegistration = hasRegistration,
            IsEmailVerified = reader.GetFieldValue<bool>(os.IsEmailVerified),
            IsSmsVerified = reader.GetFieldValue<bool>(os.IsSmsVerified),
            OperatingSystem = reader.GetString(os.OperatingSystem),
            InsertedOn = reader.GetFieldValue<DateTimeOffset>(os.InsertedOn),
            UpdatedOn = reader.GetFieldValue<DateTimeOffset?>(os.UpdatedOn),
            IsActive = reader.GetFieldValue<bool>(os.IsActive),
            OwningPersonId = reader.GetFieldValue<int?>(os.OwningPersonId),
            GroupShellId = reader.GetFieldValue<int?>(os.GroupShellId),
            OtpEmail = hasRegistration ? reader.GetString(os.OtpEmail) : string.Empty,
            OtpCellPhone = hasRegistration ? reader.GetString(os.OtpCellPhone) : string.Empty,
            RegistrationInsertedOn = hasRegistration ? reader.GetFieldValue<DateTimeOffset?>(os.RegistrationInsertedOn) : null,
            RegistrationUpdatedOn = hasRegistration ? reader.GetFieldValue<DateTimeOffset?>(os.RegistrationUpdatedOn) : null
        };
    }

    /// <summary>
    /// Maps the current row of <paramref name="reader"/> -- the result of
    /// <see cref="GetByPersonSourceMachineUuidSql"/> -- to a <see cref="SourceMachineRegistrations"/>.
    /// No Registrations join here (this query resolves device identity via PersonsSourceMachines/
    /// Persons, not the device's own OTP history), so the registration-specific fields are defaulted
    /// the same way <see cref="ToNewSourceMachineRegistration"/> defaults them for a brand-new
    /// device -- callers of this method only need SourceMachineId and the person-sourced
    /// IsEmailVerified/IsSmsVerified for claims-building, not OTP state.
    /// </summary>
    public static SourceMachineRegistrations ToSourceMachineRegistrationViaPerson(this NpgsqlDataReader reader)
    {
        return new SourceMachineRegistrations
        {
            RegistrationId = 0,
            SourceMachineId = reader.GetInt32(os.SourceMachineId),
            SourceMachineUuid = reader.GetGuid(os.SourceMachineUuid),
            SourceMachineName = reader.GetString(os.SourceMachineName),
            DeviceTypeId = (DeviceTypes)reader.GetInt32(os.DeviceTypeId),
            DisambiguationKey = reader.GetString(os.DisambiguationKey),
            EmailAddress = reader.GetString(os.EmailAddress),
            CellPhoneNumber = reader.GetString(os.CellPhoneNumber)!,
            FirstName = reader.GetString(os.FirstName),
            LastName = reader.GetString(os.LastName),
            HasRegistration = false,
            IsEmailVerified = reader.GetFieldValue<bool>(os.IsEmailVerified),
            IsSmsVerified = reader.GetFieldValue<bool>(os.IsSmsVerified),
            OperatingSystem = reader.GetString(os.OperatingSystem),
            InsertedOn = reader.GetFieldValue<DateTimeOffset>(os.InsertedOn),
            UpdatedOn = reader.GetFieldValue<DateTimeOffset?>(os.UpdatedOn),
            IsActive = reader.GetFieldValue<bool>(os.IsActive),
            OwningPersonId = reader.GetFieldValue<int?>(os.OwningPersonId),
            GroupShellId = reader.GetFieldValue<int?>(os.GroupShellId),
            OtpEmail = string.Empty,
            OtpCellPhone = string.Empty,
            RegistrationInsertedOn = null,
            RegistrationUpdatedOn = null
        };
    }

    /// <summary>
    /// Maps the current row of <paramref name="reader"/> — the RETURNING clause of
    /// <see cref="AddBySourceInformationSql"/> — to a newly created <see cref="SourceMachineRegistrations"/>.
    /// That INSERT only touches the <c>SourceMachineRegistrations</c> table, so there is no
    /// linked <c>Registrations</c> (OTP) row yet; registration-specific fields are defaulted here
    /// and populated moments later once <see cref="AddRegistrationBySourceMachineUuidSql"/> runs.
    /// </summary>
    public static SourceMachineRegistrations ToNewSourceMachineRegistration(this NpgsqlDataReader reader)
    {
        return new SourceMachineRegistrations
        {
            RegistrationId = 0,
            SourceMachineId = reader.GetInt32(os.SourceMachineId),
            SourceMachineUuid = reader.GetGuid(os.SourceMachineUuid),
            SourceMachineName = reader.GetString(os.SourceMachineName),
            DeviceTypeId = (DeviceTypes)reader.GetInt32(os.DeviceTypeId),
            DisambiguationKey = reader.GetString(os.DisambiguationKey),
            EmailAddress = reader.GetString(os.EmailAddress),
            CellPhoneNumber = reader.GetString(os.CellPhoneNumber)!,
            FirstName = reader.GetString(os.FirstName),
            LastName = reader.GetString(os.LastName),
            HasRegistration = false,
            IsEmailVerified = false,
            IsSmsVerified = false,
            OperatingSystem = reader.GetString(os.OperatingSystem),
            InsertedOn = reader.GetFieldValue<DateTimeOffset>(os.InsertedOn),
            UpdatedOn = null,
            IsActive = reader.GetFieldValue<bool>(os.IsActive),
            OtpEmail = string.Empty,
            OtpCellPhone = string.Empty,
            RegistrationInsertedOn = null,
            RegistrationUpdatedOn = null
        };
    }

    /// <summary>
    /// Maps the current row of <paramref name="reader"/> onto <paramref name="baseline"/>, for result
    /// sets — like <see cref="UpdateSourceInformationSql"/>'s — that only return the
    /// <c>SourceMachineRegistrations</c> table's own columns. Verification state, OTP codes, and the
    /// registration row's id/timestamps aren't part of that RETURNING clause, so they're carried over
    /// from <paramref name="baseline"/> (typically a row already fetched moments earlier) unchanged.
    /// </summary>
    public static SourceMachineRegistrations ToSourceMachineRegistration(this NpgsqlDataReader reader, SourceMachineRegistrations baseline)
    {
        return baseline with
        {
            SourceMachineUuid = reader.GetGuid(os.SourceMachineUuid),
            SourceMachineName = reader.GetString(os.SourceMachineName),
            DeviceTypeId = (DeviceTypes)reader.GetInt32(os.DeviceTypeId),
            EmailAddress = reader.GetString(os.EmailAddress),
            CellPhoneNumber = reader.GetString(os.CellPhoneNumber)!,
            FirstName = reader.GetString(os.FirstName),
            LastName = reader.GetString(os.LastName),
            OperatingSystem = reader.GetString(os.OperatingSystem),
            InsertedOn = reader.GetFieldValue<DateTimeOffset>(os.InsertedOn),
            IsActive = reader.GetFieldValue<bool>(os.IsActive)
        };
    }

    public static async Task<SortedSet<int>> ToRegistrationIds(this NpgsqlDataReader reader)
    {
        SortedSet<int> ids = [];
        while (await reader.ReadAsync())
        {
            ids.Add(reader.GetInt32(os.Id));
        }

        return ids;
    }

    public static async Task<SourceInformationResponse?> ToSourceInformationResponse(this NpgsqlDataReader reader)
    {
        if (!await reader.ReadAsync())
            return null;

        return new SourceInformationResponse
        {
            SourceMachineUuid = reader.GetGuid(os.SourceMachineUuid),
            SourceMachineName = reader.GetString(os.SourceMachineName),
            DeviceTypeId = (DeviceTypes)reader.GetInt32(os.DeviceTypeId),
            DisambiguationKey = reader.GetString(os.DisambiguationKey),
            EmailAddress = reader.GetString(os.EmailAddress),
            CellPhoneNumber = reader.GetString(os.CellPhoneNumber),
            FirstName = reader.GetString(os.FirstName),
            LastName = reader.GetString(os.LastName),
            OperatingSystem = reader.GetString(os.OperatingSystem),
            InsertedOn = reader.GetFieldValue<DateTimeOffset>(os.InsertedOn),
            IsActive = reader.GetFieldValue<bool>(os.IsActive)
        };
    }

    /// <summary>
    /// Maps a Cassandra/Scylla <paramref name="row"/> to a <see cref="SourceMachineRegistrations"/>.
    /// Unlike the LEFT-JOIN-based Postgres mapping, every row this table holds was itself only ever
    /// written once a joined Registrations row existed (see RegistrationsCdcSyncHandler), so
    /// <see cref="SourceMachineRegistrations.HasRegistration"/> is unconditionally true here.
    /// </summary>
    public static SourceMachineRegistrations ToSourceMachineRegistration(this Row row)
    {
        return new SourceMachineRegistrations
        {
            RegistrationId = row.GetValue<int>(ccr.RegistrationId),
            SourceMachineId = row.GetValue<int>(ccr.SourceMachineId),
            SourceMachineUuid = row.GetValue<Guid>(ccr.SourceMachineUuid),
            SourceMachineName = row.GetValue<string>(ccr.SourceMachineName),
            DeviceTypeId = (DeviceTypes)row.GetValue<int>(ccr.DeviceTypeId),
            DisambiguationKey = row.GetValue<string>(ccr.DisambiguationKey),
            EmailAddress = row.GetValue<string>(ccr.EmailAddress),
            CellPhoneNumber = row.GetValue<string>(ccr.CellPhoneNumber),
            FirstName = row.GetValue<string>(ccr.FirstName),
            LastName = row.GetValue<string>(ccr.LastName),
            HasRegistration = true,
            IsEmailVerified = row.GetValue<bool>(ccr.IsEmailVerified),
            IsSmsVerified = row.GetValue<bool>(ccr.IsSmsVerified),
            OperatingSystem = row.GetValue<string>(ccr.OperatingSystem),
            InsertedOn = row.GetValue<DateTimeOffset>(ccr.SourceInsertedOn),
            UpdatedOn = row.GetValue<DateTimeOffset?>(ccr.SourceUpdatedOn),
            IsActive = row.GetValue<bool>(ccr.IsActive),
            OtpEmail = row.GetValue<string>(ccr.OtpEmail),
            OtpCellPhone = row.GetValue<string>(ccr.OtpCellPhone),
            RegistrationInsertedOn = row.GetValue<DateTimeOffset?>(ccr.RegistrationInsertedOn),
            RegistrationUpdatedOn = row.GetValue<DateTimeOffset?>(ccr.RegistrationUpdatedOn)
        };
    }

    /// <summary>Maps the current row of <paramref name="reader"/> to an <see cref="AddRegistrationResponse"/>.</summary>
    public static AddRegistrationResponse ToAddRegistrationResponse(this NpgsqlDataReader reader)
    {
        return new AddRegistrationResponse
        {
            Id = reader.GetInt32(os.Id),
            OtpEmail = reader.GetString(os.OtpEmail),
            OtpCellPhone = reader.GetString(os.OtpCellPhone),
            IsEmailVerified = reader.GetFieldValue<bool>(os.IsEmailVerified),
            IsSmsVerified = reader.GetFieldValue<bool>(os.IsSmsVerified),
            InsertedOn = reader.GetFieldValue<DateTimeOffset>(os.InsertedOn),
            UpdatedOn = reader.GetFieldValue<DateTimeOffset?>(os.UpdatedOn)
        };
    }
}
