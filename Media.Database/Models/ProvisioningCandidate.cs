namespace Media.Database.Models;

/// <summary>
/// An active device verified on both channels that still has no group shell (WORKER-16): the
/// state in which its encryption key is owed. Carries what delivering that key needs, read from
/// Postgres, where KeyDeliveryMethod lives.
/// </summary>
public record ProvisioningCandidate
{
    public required int SourceMachineId { get; init; }

    public required string EmailAddress { get; init; }

    public required string CellPhoneNumber { get; init; }

    /// <summary>Gets the channel the registrant chose; null means the default, SMS.</summary>
    public KeyDeliveryMethods? KeyDeliveryMethod { get; init; }
}
