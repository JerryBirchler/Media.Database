using Media.Database.Models;

namespace Media.Database.Repositories;

public interface IRegistrationRepository
{
    Task<SourceMachineRegistrations?> AddBySourceInformation(AddSourceInformationRequest request);
    Task<SourceMachineRegistrations?> UpdateSourceInformation(UpdateSourceInformationRequest request);
    Task<SourceMachineRegistrations?> GetByUuid(Guid uuid);

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
}
