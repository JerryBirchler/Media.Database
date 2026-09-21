using System.Text.Json.Serialization;

namespace Media.Database.Models;

/// <summary>
/// One public key enrolled for one device. The server only ever holds the public half, so a dump of
/// this table lets nobody authenticate as anything -- the private key never leaves the device.
/// Revocation is a state change (<see cref="IsActive"/> plus <see cref="RevokedOn"/>) rather than a
/// delete, so it stays possible to answer whether a given key was trusted at a given time.
/// </summary>
public record SourceMachineKey
{
    /// <summary>
    /// Gets the integer identifier for this key. Not <c>required</c>; see
    /// <see cref="Group.GroupId"/> for why.
    /// </summary>
    [JsonIgnore]
    public int SourceMachineKeyId { get; init; }

    /// <summary>
    /// Gets the unique identifier for this key -- what a revocation request names, so the integer
    /// id never has to be exposed.
    /// </summary>
    public required Guid SourceMachineKeyUuid { get; init; }

    /// <summary>
    /// Gets the device this key belongs to. Not <c>required</c>; see
    /// <see cref="SourceMachineKeyId"/> for why.
    /// </summary>
    [JsonIgnore]
    public int SourceMachineId { get; init; }

    /// <summary>
    /// Gets whether this is the device's operational key or its pre-provisioned recovery key.
    /// </summary>
    public required SourceMachineKeyPurpose KeyPurpose { get; init; }

    /// <summary>
    /// Gets the signature algorithm this key is for.
    /// </summary>
    public required SourceMachineKeyAlgorithm Algorithm { get; init; }

    /// <summary>
    /// Gets the public key, Base64-encoded. Unique across every row in the table, including revoked
    /// ones, so a key that was ever enrolled can never be enrolled again -- that is what makes
    /// revocation final instead of something an attacker undoes by re-enrolling a stolen key.
    /// </summary>
    public required string PublicKey { get; init; }

    /// <summary>
    /// Gets a value indicating whether this key may still be used to authenticate. More than one
    /// active operational key is legal: rotation has to accept the replacement before revoking the
    /// key being replaced, or an offline device would be locked out.
    /// </summary>
    public required bool IsActive { get; init; } = true;

    /// <summary>
    /// Gets when this key was revoked, or <see langword="null"/> if it never was.
    /// </summary>
    public required DateTimeOffset? RevokedOn { get; init; }

    /// <summary>
    /// Gets the timestamp when this key was enrolled.
    /// </summary>
    public required DateTimeOffset InsertedOn { get; init; }

    /// <summary>
    /// Gets the timestamp when this key was last updated.
    /// </summary>
    public required DateTimeOffset? UpdatedOn { get; init; }
}
