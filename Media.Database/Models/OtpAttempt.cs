namespace Media.Database.Models;

/// <summary>
/// How many tries one one-time-password nonce has had, and whether it has been used (enrollment,
/// MEDIA-40). One-time codes are derived from a signed nonce and never stored, so this is the only
/// state the scheme keeps. Scylla only, written directly with a TTL of the verification window, so
/// each row disappears by itself. Holds no PII and no code.
/// </summary>
public record OtpAttempt
{
    /// <summary>
    /// Gets the nonce this counts tries for, qualified by channel (for example
    /// <c>"{nonce}:email"</c>), so each channel has its own allowance. Partition key.
    /// </summary>
    public required string NonceId { get; init; }

    /// <summary>Gets the number of wrong tries so far.</summary>
    public required int Attempts { get; init; }

    /// <summary>Gets whether a correct code has been accepted; a used nonce accepts nothing more.</summary>
    public required bool IsUsed { get; init; }

    /// <summary>Gets when the nonce was issued.</summary>
    public required DateTimeOffset IssuedOn { get; init; }
}
