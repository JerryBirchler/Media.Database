namespace Media.Database.Models;

/// <summary>
/// Where an active device's group encryption key is anchored, and how that key is delivered --
/// read from Postgres, because the Scylla registrations table carries neither, so
/// <c>IRegistrationRepository.GetByIdsAsync</c> always reports them as unset.
/// </summary>
public record DeviceKeyAnchor
{
    /// <summary>Gets the group shell the device's key belongs to; null until one is provisioned.</summary>
    public int? GroupShellId { get; init; }

    /// <summary>Gets the delivery channel the registrant chose; null means the default, SMS.</summary>
    public KeyDeliveryMethods? KeyDeliveryMethod { get; init; }
}
