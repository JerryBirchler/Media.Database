namespace Media.Database.Models;

/// <summary>
/// Resolves whether encryption at rest is currently in effect for a device -- the single source
/// of truth for the COALESCE(device, group) rule (MEDIA-11): an explicit device-level override
/// always wins over the device's group's policy.
/// </summary>
public static class EncryptionPolicy
{
    public static bool ResolveEffective(SourceMachineRegistrations device, Group group) =>
        device.IsEncrypted ?? group.IsEncrypted;
}
