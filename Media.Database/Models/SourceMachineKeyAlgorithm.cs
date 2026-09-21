namespace Media.Database.Models;

/// <summary>
/// The signature algorithm a device's public key is for. Stored per key rather than assumed
/// globally so a future algorithm can be introduced and devices migrated individually, instead of
/// every device having to change on the same day.
/// </summary>
public enum SourceMachineKeyAlgorithm
{
    /// <summary>Ed25519 (EdDSA over Curve25519).</summary>
    Ed25519
}
