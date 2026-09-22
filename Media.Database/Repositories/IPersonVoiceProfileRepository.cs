using Media.Database.Models;

namespace Media.Database.Repositories;

/// <summary>
/// Storage for the speaker-recognition profiles people have opted into.
///
/// Every method is keyed by <c>PersonId</c> rather than by the profile's own identifier, because a
/// person has at most one active profile and the caller always knows who they mean. That also
/// keeps the biometric out of any path where a profile could be addressed without knowing whose it
/// is.
/// </summary>
public interface IPersonVoiceProfileRepository
{
    /// <summary>
    /// Records a new profile for a person, with the moment they consented.
    /// </summary>
    /// <param name="personId">The person the profile belongs to.</param>
    /// <param name="provider">The implementation that produced <paramref name="profileData"/>.</param>
    /// <param name="profileData">The provider's own representation of the voice.</param>
    /// <param name="consentedOn">When the person agreed to this.</param>
    Task<PersonVoiceProfile?> AddAsync(int personId, SpeakerRecognitionProviders provider, string profileData, DateTimeOffset consentedOn);

    /// <summary>Reads a person's active profile, or null when they have not enrolled.</summary>
    Task<PersonVoiceProfile?> GetActiveByPersonIdAsync(int personId);

    /// <summary>
    /// Reads the active profiles of several people at once -- the identification path, which
    /// compares one utterance against only the people a caller could actually be.
    /// </summary>
    Task<List<PersonVoiceProfile>> GetActiveByPersonIdsAsync(IEnumerable<int> personIds);

    /// <summary>
    /// Withdraws a person's profile, returning what was revoked or null when there was nothing
    /// active. Idempotent: revoking twice leaves the first revocation's timestamp intact.
    /// </summary>
    Task<PersonVoiceProfile?> RevokeByPersonIdAsync(int personId);
}
