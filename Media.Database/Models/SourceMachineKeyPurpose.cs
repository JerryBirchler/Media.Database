using System.Text.Json.Serialization;

namespace Media.Database.Models;

/// <summary>
/// What role a device's enrolled keypair plays. A device holds one operational key it authenticates
/// with day to day, and may hold a <see cref="Recovery"/> key whose private half is deliberately
/// kept somewhere else -- printed, in a password manager, or on a second device -- so a device that
/// loses its operational private key can re-authenticate without an OTP round trip.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SourceMachineKeyPurpose
{
    /// <summary>The key the device signs with normally.</summary>
    Operational,

    /// <summary>A pre-provisioned spare, used only to recover from loss of the operational key.</summary>
    Recovery
}
