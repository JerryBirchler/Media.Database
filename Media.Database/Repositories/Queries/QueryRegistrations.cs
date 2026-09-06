using Media.Database.Helpers;
using Media.Database.Models;
using Npgsql;

#pragma warning disable CS8981
using ccr = Media.Database.Repositories.Schemas.TablesCql.RegistrationsColumns;
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
            {cssmr.EmailAddress}, 
            {cssmr.CellPhoneNumber}, 
            {cssmr.FirstName}, 
            {cssmr.LastName}, 
            {cssmr.OperatingSystem}
        ) VALUES (
            {pn.SourceMachineName}, 
            {pn.DeviceTypeId}, 
            {pn.EmailAddress}, 
            {pn.CellPhoneNumber}, 
            {pn.FirstName}, 
            {pn.LastName}, 
            {pn.OperatingSystem}
        )
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
    /// SQL to select a SourceMachine by its source machine name, device type, email address, cell phone number, first name, and last name.
    /// </summary>
    public static string GetBySourceInformationSql => $@"
        SELECT  
            {csr.Id},
            {cssmr.SourceMachineId}, 
            {cssmr.SourceMachineUuid}, 
            {cssmr.SourceMachineName}, 
            {cssmr.DeviceTypeId}, 
            {cssmr.EmailAddress}, 
            {cssmr.CellPhoneNumber}, 
            {cssmr.FirstName}, 
            {cssmr.LastName}, 
            CASE WHEN r.Id IS NULL THEN False ELSE True END AS ""HasRegistration"",
            COALESCE({csr.IsEmailVerified}, False) AS ""IsEmailVerified"", 
            COALESCE({csr.IsSmsVerified}, False) AS ""IsSmsVerified"", 
            {cssmr.OperatingSystem}, 
            {cssmr.IsActive}, 
            {cssmr.InsertedOn}, 
            {cssmr.UpdatedOn},
            {csr.OtpEmail},
            {csr.OtpCellPhone},
            {csr.InsertedOn} As ""RegistrationInsertedOn"",
            {csr.UpdatedOn} AS ""RegistrationUpdatedOn""
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
            AND smr.{cssmr.EmailAddress} = {pn.EmailAddress} 
            AND smr.{cssmr.CellPhoneNumber} = {pn.CellPhoneNumber}
            AND smr.{cssmr.FirstName} = {pn.FirstName}
            AND smr.{cssmr.LastName} = {pn.LastName}
        LIMIT 1 
        ;";


    /// <summary>SQL to select a SourceMachine by its unique identifier.</summary>
    public static string GetBySourceMachineUuidSql => $@"
        SELECT 
            {csr.Id},
            {cssmr.SourceMachineId}, 
            {cssmr.SourceMachineUuid}, 
            {cssmr.SourceMachineName}, 
            {cssmr.DeviceTypeId}, 
            {cssmr.EmailAddress}, 
            {cssmr.CellPhoneNumber}, 
            {cssmr.FirstName}, 
            {cssmr.LastName}, 
            CASE WHEN r.Id IS NULL THEN False ELSE True END AS ""HasRegistration"",
            COALESCE({csr.IsEmailVerified}, False) AS ""IsEmailVerified"", 
            COALESCE({csr.IsSmsVerified}, False) AS ""IsSmsVerified"", 
            {cssmr.OperatingSystem}, 
            {cssmr.IsActive}, 
            {cssmr.InsertedOn}, 
            {cssmr.UpdatedOn}
            {csr.OtpEmail},
            {csr.OtpCellPhone},
            {csr.InsertedOn} As ""RegistrationInsertedOn"",
            {csr.UpdatedOn} AS ""RegistrationUpdatedOn""
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
            {csr.Id},
            {cssmr.SourceMachineId},
            {cssmr.SourceMachineUuid},
            {cssmr.SourceMachineName},
            {cssmr.DeviceTypeId},
            {cssmr.EmailAddress},
            {cssmr.CellPhoneNumber},
            {cssmr.FirstName},
            {cssmr.LastName},
            CASE WHEN r.Id IS NULL THEN False ELSE True END AS ""HasRegistration"",
            COALESCE({csr.IsEmailVerified}, False) AS ""IsEmailVerified"",
            COALESCE({csr.IsSmsVerified}, False) AS ""IsSmsVerified"",
            {cssmr.OperatingSystem},
            {cssmr.IsActive},
            {cssmr.InsertedOn},
            {cssmr.UpdatedOn}
            {csr.OtpEmail},
            {csr.OtpCellPhone},
            {csr.InsertedOn} As ""RegistrationInsertedOn"",
            {csr.UpdatedOn} AS ""RegistrationUpdatedOn""
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
    /// Inactivate current registrations by source machine UUID, 
    /// and return the updated registration Ids.
    /// </summary>
    public static string InactivateRegistrationsBySourceMachineUuidSql => $@"
        UPDATE r SET 
            {csr.IsCurrent} = False,
            {csr.UpdatedOn} = {pn.UpdatedOn}
        FROM 
            {ts.Registrations} AS r
        INNER JOIN 
            {ts.SourceMachineRegistrations} AS smr
        ON  
            r.{csr.SourceMachineId} = smr.{cssmr.SourceMachineId}
        WHERE 
            smr.{cssmr.SourceMachineUuid} = {pn.SourceMachineUuid}
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
            {csr.IsSmsVerified}
        ) 
        SELECT
            {cssmr.SourceMachineId}, 
            {cssmr.EmailAddress}, 
            {pn.OtpEmail}, 
            CASE WHEN {pn.OtpEmail} = '' THEN True ELSE False END,
            {cssmr.CellPhoneNumber}, 
            {pn.OtpCellPhone},
            CASE WHEN {pn.OtpCellPhone} = '' THEN True ELSE False END
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
        UPDATE r SET
            {csr.IsEmailVerified} = True,
            {csr.UpdatedOn} = {pn.UpdatedOn}
        FROM
            {ts.Registrations} AS r
        INNER JOIN
            {ts.SourceMachineRegistrations} AS smr
        ON
            r.{csr.SourceMachineId} = smr.{cssmr.SourceMachineId}
        WHERE
            {cssmr.SourceMachineUuid} = {pn.SourceMachineUuid}
            AND {csr.IsCurrent} = True
            AND {csr.EmailAddress} = {cssmr.EmailAddress}
            AND {csr.CellPhoneNumber} = {cssmr.CellPhoneNumber}
            AND {csr.OtpEmail} = {pn.OtpEmail}
            AND {csr.InsertedOn} > {pn.OtpWindowStart}
        RETURNING
            smr.{cssmr.SourceMachineUuid},
            smr.{cssmr.SourceMachineName},
            smr.{cssmr.DeviceTypeId},
            smr.{cssmr.FirstName},
            smr.{cssmr.LastName},
            r.{csr.EmailAddress},
            r.{csr.IsEmailVerified}
        ;";

    /// <summary>
    /// Update the registration when the one-time password for cell phone is verified. The OTP is
    /// only honored within one hour of the pending registration row's <see cref="TablesSql.RegistrationsColumns.InsertedOn"/>;
    /// past that window this matches no row, the same as an incorrect code.
    /// </summary>
    public static string VerifyOtpCellPhoneSql => $@"
        UPDATE r SET
            {csr.IsSmsVerified} = True,
            {csr.UpdatedOn} = {pn.UpdatedOn}
        FROM
            {ts.Registrations} AS r
        INNER JOIN
            {ts.SourceMachineRegistrations} AS smr
        ON
            r.{csr.SourceMachineId} = smr.{cssmr.SourceMachineId}
        WHERE
            {cssmr.SourceMachineUuid} = {pn.SourceMachineUuid}
            AND {csr.IsCurrent} = True
            AND {csr.EmailAddress} = {cssmr.EmailAddress}
            AND {csr.CellPhoneNumber} = {cssmr.CellPhoneNumber}
            AND {csr.OtpCellPhone} = {pn.OtpCellPhone}
            AND {csr.InsertedOn} > {pn.OtpWindowStart}
        RETURNING
            smr.{cssmr.SourceMachineUuid},
            smr.{cssmr.SourceMachineName},
            smr.{cssmr.DeviceTypeId},
            smr.{cssmr.FirstName},
            smr.{cssmr.LastName},
            r.{csr.CellPhoneNumber},
            r.{csr.IsSmsVerified}
        ;";

    #endregion

    #region CQL Queries
    public static string UpsertRegistrationCql => $@"
        INSERT INTO {tc.Registrations} (
            {ccr.SourceMachineUuid},
            {ccr.RegistrationId},
            {ccr.SourceMachineId},
            {ccr.SourceMachineName},
            {ccr.DeviceTypeId},
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

    /// <summary>Maps the current row of <paramref name="reader"/> to a <see cref="SourceMachineRegistrations"/>.</summary>
    public static SourceMachineRegistrations ToSourceMachineRegistration(this NpgsqlDataReader reader)
    {
        return new SourceMachineRegistrations
        {
            RegistrationId = reader.GetInt32(os.Id),
            SourceMachineId = reader.GetInt32(os.SourceMachineId),
            SourceMachineUuid = reader.GetGuid(os.SourceMachineUuid),
            SourceMachineName = reader.GetString(os.SourceMachineName),
            DeviceTypeId = (DeviceTypes)reader.GetInt32(os.DeviceTypeId),
            EmailAddress = reader.GetString(os.EmailAddress),
            CellPhoneNumber = reader.GetString(os.CellPhoneNumber)!,
            FirstName = reader.GetString(os.FirstName),
            LastName = reader.GetString(os.LastName),
            HasRegistration = reader.GetFieldValue<bool>(os.HasRegistration),
            IsEmailVerified = reader.GetFieldValue<bool>(os.IsEmailVerified),
            IsSmsVerified = reader.GetFieldValue<bool>(os.IsSmsVerified),
            OperatingSystem = reader.GetString(os.OperatingSystem),
            InsertedOn = reader.GetFieldValue<DateTimeOffset>(os.InsertedOn),
            UpdatedOn = reader.GetFieldValue<DateTimeOffset?>(os.UpdatedOn),
            IsActive = reader.GetFieldValue<bool>(os.IsActive),
            OtpEmail = reader.GetString(os.OtpEmail),
            OtpCellPhone = reader.GetString(os.OtpCellPhone),
            RegistrationInsertedOn = reader.GetFieldValue<DateTimeOffset?>(os.RegistrationInsertedOn),
            RegistrationUpdatedOn = reader.GetFieldValue<DateTimeOffset?>(os.RegistrationUpdatedOn)
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

    /// <summary>Maps the current row of <paramref name="reader"/> to an <see cref="OtpEmailResponse"/>.</summary>
    public static OtpEmailResponse ToOtpEmailResponse(this NpgsqlDataReader reader)
    {
        return new OtpEmailResponse
        {
            SourceMachineUuid = reader.GetGuid(os.SourceMachineUuid),
            SourceMachineName = reader.GetString(os.SourceMachineName),
            DeviceTypeId = (DeviceTypes)reader.GetInt32(os.DeviceTypeId),
            FirstName = reader.GetString(os.FirstName),
            LastName = reader.GetString(os.LastName),
            EmailAddress = reader.GetString(os.EmailAddress),
            OtpEmailVerified = reader.GetFieldValue<bool>(os.IsEmailVerified)
        };
    }

    /// <summary>Maps the current row of <paramref name="reader"/> to an <see cref="OtpSmsResponse"/>.</summary>
    public static OtpSmsResponse ToOtpSmsResponse(this NpgsqlDataReader reader)
    {
        return new OtpSmsResponse
        {
            SourceMachineUuid = reader.GetGuid(os.SourceMachineUuid),
            SourceMachineName = reader.GetString(os.SourceMachineName),
            DeviceTypeId = (DeviceTypes)reader.GetInt32(os.DeviceTypeId),
            FirstName = reader.GetString(os.FirstName),
            LastName = reader.GetString(os.LastName),
            CellPhoneNumber = reader.GetString(os.CellPhoneNumber),
            OtpSmsVerified = reader.GetFieldValue<bool>(os.IsSmsVerified)
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
            EmailAddress = reader.GetString(os.EmailAddress),
            CellPhoneNumber = reader.GetString(os.CellPhoneNumber),
            FirstName = reader.GetString(os.FirstName),
            LastName = reader.GetString(os.LastName),
            OperatingSystem = reader.GetString(os.OperatingSystem),
            InsertedOn = reader.GetFieldValue<DateTimeOffset>(os.InsertedOn),
            IsActive = reader.GetFieldValue<bool>(os.IsActive)
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