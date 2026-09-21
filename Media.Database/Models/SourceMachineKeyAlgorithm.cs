namespace Media.Database.Models;

/// <summary>
/// The signature algorithm a device's public key is for. Stored per key rather than assumed
/// globally so a future algorithm can be introduced and devices migrated individually, instead of
/// every device having to change on the same day.
/// </summary>
public enum SourceMachineKeyAlgorithm
{
    /// <summary>
    /// ECDSA over NIST P-256 with SHA-256 -- the JWS "ES256" algorithm. The only algorithm
    /// supported today, chosen because .NET implements it natively; Ed25519 would require a
    /// third-party crypto dependency.
    /// </summary>
    Es256,

    /// <summary>
    /// Ed25519 (EdDSA over Curve25519). Reserved, not yet accepted -- .NET has no built-in
    /// implementation, so supporting it means taking on BouncyCastle or NSec first.
    /// </summary>
    Ed25519
}
