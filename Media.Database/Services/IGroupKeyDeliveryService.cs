using Media.Database.Models;

namespace Media.Database.Services;

/// <summary>
/// Delivers a freshly generated group encryption key to the customer over the channel they chose.
/// Never falls back to another channel: the customer picks precisely so that they accept that
/// channel's risk, and a silent substitution would take that back.
/// </summary>
public interface IGroupKeyDeliveryService
{
    /// <summary>
    /// Sends <paramref name="rawKey"/> over <paramref name="keyDeliveryMethod"/>.
    /// </summary>
    /// <exception cref="KeyDeliveryUnavailableException">
    /// The chosen channel has no destination on file -- SMS with no cell phone number, or email
    /// with no address.
    /// </exception>
    /// <exception cref="NotSupportedException">
    /// The chosen channel has no sender configured on this deployment.
    /// </exception>
    Task DeliverAsync(KeyDeliveryMethods keyDeliveryMethod, string emailAddress, string cellPhoneNumber, string rawKey, CancellationToken cancellationToken = default);
}
