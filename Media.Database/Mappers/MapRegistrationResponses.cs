using Media.Database.Models;

namespace Media.Database.Mappers;

/// <inheritdoc cref="IMapRegistrationResponses"/>
public class MapRegistrationResponses : IMapRegistrationResponses
{
    public OtpEmailResponse ToOtpEmailResponse(
        Guid sourceMachineUuid,
        string sourceMachineName,
        DeviceTypes deviceTypeId,
        string firstName,
        string lastName,
        string emailAddress,
        bool isEmailVerified,
        bool isSmsVerified)
    {
        return new OtpEmailResponse
        {
            SourceMachineUuid = sourceMachineUuid,
            SourceMachineName = sourceMachineName,
            DeviceTypeId = deviceTypeId,
            FirstName = firstName,
            LastName = lastName,
            EmailAddress = emailAddress,
            OtpEmailVerified = isEmailVerified,
            ApiKey = isEmailVerified && isSmsVerified ? sourceMachineUuid : null
        };
    }

    public OtpSmsResponse ToOtpSmsResponse(
        Guid sourceMachineUuid,
        string sourceMachineName,
        DeviceTypes deviceTypeId,
        string firstName,
        string lastName,
        string cellPhoneNumber,
        bool isSmsVerified,
        bool isEmailVerified)
    {
        return new OtpSmsResponse
        {
            SourceMachineUuid = sourceMachineUuid,
            SourceMachineName = sourceMachineName,
            DeviceTypeId = deviceTypeId,
            FirstName = firstName,
            LastName = lastName,
            CellPhoneNumber = cellPhoneNumber,
            OtpSmsVerified = isSmsVerified,
            ApiKey = isEmailVerified && isSmsVerified ? sourceMachineUuid : null
        };
    }
}
