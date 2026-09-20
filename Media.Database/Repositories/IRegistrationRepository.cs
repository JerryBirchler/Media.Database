using Media.Database.Models;

namespace Media.Database.Repositories;

public interface IRegistrationRepository
{
    Task<SourceMachineRegistrations?> AddBySourceInformation(AddSourceInformationRequest request);
    Task<SourceMachineRegistrations?> UpdateSourceInformation(UpdateSourceInformationRequest request);
    Task<SourceMachineRegistrations?> GetByUuid(Guid uuid);

    /// <summary>
    /// Resolves a <c>PersonSourceMachineUuid</c> -- the multi-origin X-API-KEY model's second
    /// credential type (MEDIA-34) -- to the one device it grants access to. IsEmailVerified/
    /// IsSmsVerified on the result reflect the *person's own* verification, not the device's,
    /// since the caller is authenticating as themselves. Returns null unless the person/device
    /// association, the person, and the device are all active.
    /// </summary>
    Task<SourceMachineRegistrations?> GetByPersonSourceMachineUuid(Guid uuid);

    /// <summary>
    /// Verifies the email OTP code for the pending registration matching <paramref name="emailAddress"/>,
    /// <paramref name="sourceMachineName"/>, and <paramref name="deviceTypeId"/>. Deliberately does not
    /// take the device's UUID/X-API-KEY -- that key is never issued to a client before verification
    /// completes, so it can't be used to identify the registration being verified.
    /// </summary>
    Task<OtpEmailResponse?> VerifyOtpEmail(string emailAddress, string sourceMachineName, DeviceTypes deviceTypeId, string otp);

    /// <summary>
    /// Verifies the SMS OTP code for the pending registration matching <paramref name="cellPhoneNumber"/>,
    /// <paramref name="sourceMachineName"/>, and <paramref name="deviceTypeId"/>. Deliberately does not
    /// take the device's UUID/X-API-KEY -- see <see cref="VerifyOtpEmail"/> for why.
    /// </summary>
    Task<OtpSmsResponse?> VerifyOtpCellPhone(string cellPhoneNumber, string sourceMachineName, DeviceTypes deviceTypeId, string otp);

    /// <summary>
    /// Regenerates OTP codes for whichever of email/SMS remain unverified for the registration
    /// matching <paramref name="sourceMachineName"/>, <paramref name="deviceTypeId"/>,
    /// <paramref name="emailAddress"/>, and <paramref name="cellPhoneNumber"/> -- the same identifying
    /// tuple used at initial registration (<see cref="AddBySourceInformation"/>), since a device
    /// that has not finished verifying has no UUID/X-API-KEY to identify itself with otherwise.
    /// Leaves any already-verified channel untouched. Returns null if no registration matches.
    /// </summary>
    Task<ResendOtpResult?> ResendOtp(string sourceMachineName, DeviceTypes deviceTypeId, string emailAddress, string cellPhoneNumber);

    /// <summary>
    /// Hydrates full <see cref="SourceMachineRegistrations"/> rows for a set of source machine
    /// ids -- Postgres identifies which devices and in what order (e.g. a group's device list),
    /// this hydrates the full row content preferring the existing "registrations" Scylla table,
    /// falling back to PostgreSQL per row when Scylla doesn't have it yet (CDC lag) or is
    /// unreachable. Same split as <see cref="IGroupRepository.GetByIdsAsync"/>.
    /// </summary>
    /// <param name="sourceMachineIds">The source machine ids to hydrate.</param>
    /// <param name="maxDegreeOfParallelism">The maximum number of concurrent lookups.</param>
    /// <returns>The hydrated registrations, in no particular order -- callers that need a specific order must reorder by id themselves.</returns>
    Task<List<SourceMachineRegistrations>> GetByIdsAsync(IEnumerable<int> sourceMachineIds, int maxDegreeOfParallelism);

    /// <summary>
    /// Permanently sets <c>SourceMachineRegistrations.OwningPersonId</c> to <paramref name="personId"/>
    /// for <paramref name="sourceMachineId"/> -- a no-op if already set (MEDIA-10: device ownership
    /// is singular and permanent, never reassigned or cleared by any endpoint, through any path).
    /// </summary>
    Task SetOwningPersonIfUnsetAsync(int sourceMachineId, int personId);

    /// <summary>
    /// Sets a device's <see cref="SourceMachineRegistrations.IsEncrypted"/> override flag.
    /// Device-owner authorization is enforced by the caller, not here.
    /// </summary>
    Task SetIsEncryptedAsync(int sourceMachineId, bool isEncrypted);
}
