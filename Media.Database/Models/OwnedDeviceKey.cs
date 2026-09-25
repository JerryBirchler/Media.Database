namespace Media.Database.Models;

/// <summary>
/// For development test support only (DATABASE-34): a person's newest active device, with its key
/// and where its keys were delivered, so the web key form can fill itself in. Never served outside
/// Media.Api's TestSupport, which is off everywhere but a disposable environment.
/// </summary>
public record OwnedDeviceKey
{
    /// <summary>Gets the device's key (its uuid).</summary>
    public required Guid SourceMachineUuid { get; init; }

    public required string SourceMachineName { get; init; }

    /// <summary>Gets the email the device's keys were delivered to.</summary>
    public required string EmailAddress { get; init; }

    /// <summary>Gets the phone the device's keys were delivered to.</summary>
    public required string CellPhoneNumber { get; init; }
}
