using Media.Database.Models;

namespace Media.Database.Mappers;

/// <summary>
/// Shapes OTP verification response models from plain values (never from an
/// <see cref="Npgsql.NpgsqlDataReader"/> directly, which is sealed and can't be mocked) so the
/// decision of when to reveal a device's API key is independently unit-testable.
/// </summary>
public interface IMapRegistrationResponses
{
    /// <summary>
    /// Builds the email OTP verification response. <see cref="BaseOtpVerificationResponse.ApiKey"/>
    /// is populated only when both <paramref name="isEmailVerified"/> and
    /// <paramref name="isSmsVerified"/> are true -- the device's permanent API key is never
    /// revealed until both channels are verified.
    /// </summary>
    OtpEmailResponse ToOtpEmailResponse(
        Guid sourceMachineUuid,
        string sourceMachineName,
        DeviceTypes deviceTypeId,
        string firstName,
        string lastName,
        string emailAddress,
        bool isEmailVerified,
        bool isSmsVerified);

    /// <summary>
    /// Builds the SMS OTP verification response. <see cref="BaseOtpVerificationResponse.ApiKey"/>
    /// is populated only when both <paramref name="isSmsVerified"/> and
    /// <paramref name="isEmailVerified"/> are true -- see <see cref="ToOtpEmailResponse"/>.
    /// </summary>
    OtpSmsResponse ToOtpSmsResponse(
        Guid sourceMachineUuid,
        string sourceMachineName,
        DeviceTypes deviceTypeId,
        string firstName,
        string lastName,
        string cellPhoneNumber,
        bool isSmsVerified,
        bool isEmailVerified);
}
