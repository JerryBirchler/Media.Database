using System.Text.Json.Serialization;

namespace Media.Database.Models;

/// <summary>
/// Which channel a customer chose to receive their generated group encryption key on. The customer
/// picks rather than the system deciding, deliberately: it moves acceptance of the channel's risk
/// to them. <see cref="Sms"/> is the default when no choice is expressed.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum KeyDeliveryMethods
{
    /// <summary>Send the key by SMS. The default, and the recommended channel.</summary>
    Sms,

    /// <summary>Send the key by email.</summary>
    Email
}
