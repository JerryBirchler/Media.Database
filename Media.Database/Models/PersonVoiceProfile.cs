using System.Text.Json.Serialization;

namespace Media.Database.Models;

/// <summary>
/// A person's opted-in speaker-recognition profile, used to tell who is speaking on a device more
/// than one person uses. Never holds audio.
///
/// <see cref="ProfileData"/> is opaque here by design -- only the provider that wrote it
/// interprets it. Self-hosted it is a serialized speaker embedding, which is a template rather
/// than a recording and is not reversible to audio. A managed provider would instead hold the
/// biometric and leave a profile identifier here. One shape serves both, so changing provider
/// needs no migration.
/// </summary>
public record PersonVoiceProfile
{
    public int PersonVoiceProfileId { get; init; }

    /// <summary>
    /// The profile's external identifier. Never a credential -- unlike a person's own uuid, this
    /// grants nothing; it exists so a caller can name a profile to revoke without being handed
    /// anything that authenticates.
    /// </summary>
    public Guid PersonVoiceProfileUuid { get; init; }

    public int PersonId { get; init; }

    /// <summary>Which implementation produced <see cref="ProfileData"/>, and so which can read it.</summary>
    public SpeakerRecognitionProviders Provider { get; init; }

    /// <summary>
    /// The provider's own representation of this voice. Never logged, never returned to a caller:
    /// it is the biometric artifact itself.
    /// </summary>
    [JsonIgnore]
    public required string ProfileData { get; init; }

    /// <summary>
    /// When this person consented to their voice being used this way.
    ///
    /// Not derived from <see cref="InsertedOn"/>, though they will usually match. Consent has to
    /// be demonstrable on its own terms, and a row's creation timestamp is a storage fact rather
    /// than a record of someone agreeing to something.
    /// </summary>
    public DateTimeOffset ConsentedOn { get; init; }

    public bool IsActive { get; init; }

    /// <summary>
    /// When the person withdrew. Revoked rows are kept rather than deleted so withdrawal is
    /// auditable, and re-enrolling afterwards writes a new row instead of reviving this one.
    /// </summary>
    public DateTimeOffset? RevokedOn { get; init; }

    public DateTimeOffset InsertedOn { get; init; }

    public DateTimeOffset? UpdatedOn { get; init; }
}

/// <summary>
/// The speaker-recognition implementations a profile can come from. Stored rather than inferred,
/// because a stored profile is only readable by the implementation that produced it.
/// </summary>
public enum SpeakerRecognitionProviders
{
    /// <summary>
    /// A speaker embedding computed by a model we run ourselves. The biometric never leaves our
    /// infrastructure, which is both the cheapest option and the strongest custody position.
    /// </summary>
    SelfHosted = 1,

    /// <summary>
    /// A managed speaker-recognition service holding the biometric on our behalf, leaving only an
    /// identifier here. Not in use; present so that adopting one is a new enum value rather than a
    /// schema change.
    /// </summary>
    Azure = 2
}
