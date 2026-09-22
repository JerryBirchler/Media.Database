using Media.Database.Helpers;
using Media.Database.Models;
using Media.Database.Repositories.Queries.Helpers;
using Npgsql;
using cv = Media.Database.Repositories.Schemas.TablesSql.PersonVoiceProfilesColumns;
using os = Media.Database.Repositories.Schemas.OrdinalsSql;
using pn = Media.Database.Repositories.Schemas.ParameterNames;
using ts = Media.Database.Repositories.Schemas.TablesSql;

namespace Media.Database.Repositories.Queries;

/// <summary>
/// SQL for the speaker-recognition profiles a person has opted into.
///
/// PostgreSQL only, deliberately. Unlike Persons or Files there is no Scylla copy and no CDC
/// handler: a profile is read on the identification path rather than a paginated one, so it gains
/// nothing from hydration -- and a biometric artifact is better held in one place than replicated
/// into a second store, which is the same argument that took SourceMachineUuid out of Scylla.
/// </summary>
public static class QueryPersonVoiceProfiles
{
    /// <summary>
    /// SQL to record a new profile. Always an insert, never an update: revoking and re-enrolling
    /// leaves both rows, so a withdrawal remains visible afterwards.
    /// </summary>
    public static string AddSql => $@"
        INSERT INTO {ts.PersonVoiceProfiles} (
            {cv.PersonId},
            {cv.Provider},
            {cv.ProfileData},
            {cv.ConsentedOn}
        ) VALUES (
            {pn.PersonId},
            {pn.Provider},
            {pn.ProfileData},
            {pn.ConsentedOn}
        )
        RETURNING
            {cv.PersonVoiceProfileId},
            {cv.PersonVoiceProfileUuid},
            {cv.PersonId},
            {cv.Provider},
            {cv.ProfileData},
            {cv.ConsentedOn},
            {cv.IsActive},
            {cv.RevokedOn},
            {cv.InsertedOn},
            {cv.UpdatedOn}
        ;";

    /// <summary>SQL to read a person's active profile, if they have one.</summary>
    public static string GetActiveByPersonIdSql => $@"
        SELECT
            {cv.PersonVoiceProfileId},
            {cv.PersonVoiceProfileUuid},
            {cv.PersonId},
            {cv.Provider},
            {cv.ProfileData},
            {cv.ConsentedOn},
            {cv.IsActive},
            {cv.RevokedOn},
            {cv.InsertedOn},
            {cv.UpdatedOn}
        FROM {ts.PersonVoiceProfiles}
        WHERE
            {cv.PersonId} = {pn.PersonId}
            AND {cv.IsActive}
        ;";

    /// <summary>
    /// SQL to read the active profiles of several people at once -- the identification path, which
    /// compares one utterance against the candidates a caller can actually be.
    /// </summary>
    public static string GetActiveByPersonIdsSql => $@"
        SELECT
            {cv.PersonVoiceProfileId},
            {cv.PersonVoiceProfileUuid},
            {cv.PersonId},
            {cv.Provider},
            {cv.ProfileData},
            {cv.ConsentedOn},
            {cv.IsActive},
            {cv.RevokedOn},
            {cv.InsertedOn},
            {cv.UpdatedOn}
        FROM {ts.PersonVoiceProfiles}
        WHERE
            {cv.PersonId} = ANY({pn.PersonId})
            AND {cv.IsActive}
        ;";

    /// <summary>
    /// SQL to withdraw a person's profile. Set-once on the way out: the WHERE clause requires the
    /// row to still be active, so a second revocation changes nothing rather than overwriting the
    /// moment the first one happened.
    /// </summary>
    public static string RevokeByPersonIdSql => $@"
        UPDATE {ts.PersonVoiceProfiles} SET
            {cv.IsActive} = false,
            {cv.RevokedOn} = {pn.RevokedOn},
            {cv.UpdatedOn} = {pn.UpdatedOn}
        WHERE
            {cv.PersonId} = {pn.PersonId}
            AND {cv.IsActive}
        RETURNING
            {cv.PersonVoiceProfileId},
            {cv.PersonVoiceProfileUuid},
            {cv.PersonId},
            {cv.Provider},
            {cv.ProfileData},
            {cv.ConsentedOn},
            {cv.IsActive},
            {cv.RevokedOn},
            {cv.InsertedOn},
            {cv.UpdatedOn}
        ;";
}

/// <summary>Row mapping for <see cref="QueryPersonVoiceProfiles"/>.</summary>
public static class PersonVoiceProfileMappers
{
    /// <summary>Maps the current row of <paramref name="reader"/> to a <see cref="PersonVoiceProfile"/>.</summary>
    public static PersonVoiceProfile ToPersonVoiceProfile(this NpgsqlDataReader reader)
    {
        return new PersonVoiceProfile
        {
            PersonVoiceProfileId = reader.GetInt32(os.PersonVoiceProfileId),
            PersonVoiceProfileUuid = reader.GetGuid(os.PersonVoiceProfileUuid),
            PersonId = reader.GetInt32(os.PersonId),
            Provider = (SpeakerRecognitionProviders)reader.GetInt32(os.Provider),
            ProfileData = reader.GetString(os.ProfileData),
            ConsentedOn = reader.GetFieldValue<DateTimeOffset>(os.ConsentedOn),
            IsActive = reader.GetFieldValue<bool>(os.IsActive),
            RevokedOn = reader.GetFieldValue<DateTimeOffset?>(os.RevokedOn),
            InsertedOn = reader.GetFieldValue<DateTimeOffset>(os.InsertedOn),
            UpdatedOn = reader.GetFieldValue<DateTimeOffset?>(os.UpdatedOn)
        };
    }
}
