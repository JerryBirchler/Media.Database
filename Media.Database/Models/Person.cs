using Media.Common.Serialization;
using System.Text.Json.Serialization;

namespace Media.Database.Models;

/// <summary>
/// Represents a person, derived from a device's verified registration contact information.
/// </summary>
public record Person
{
    /// <summary>
    /// Gets the integer identifier for the person. Not <c>required</c>; see
    /// <see cref="Group.GroupId"/> for why.
    /// </summary>
    [JsonIgnore]
    public int PersonId { get; init; }

    /// <summary>
    /// Gets the unique identifier for the person. Omitted from JSON entirely when redacted (zeroed
    /// to <see cref="Guid.Empty"/>, which is also <c>Guid</c>'s CLR default) -- not just replaced
    /// with a visible sentinel value.
    /// </summary>
    [Redactable]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public required Guid PersonUuid { get; init; }

    /// <summary>
    /// Gets the person's email address.
    /// </summary>
    public required string EmailAddress { get; init; } = string.Empty;

    /// <summary>
    /// Gets the person's cell phone number.
    /// </summary>
    public required string CellPhoneNumber { get; init; } = string.Empty;

    /// <summary>
    /// Gets the person's first name.
    /// </summary>
    public required string FirstName { get; init; } = string.Empty;

    /// <summary>
    /// Gets the person's last name.
    /// </summary>
    public required string LastName { get; init; } = string.Empty;

    /// <summary>
    /// Gets a value indicating whether the person is active.
    /// </summary>
    public required bool IsActive { get; init; } = true;

    /// <summary>
    /// Gets the identifier of the person who created this person, or <see langword="null"/> for a
    /// person auto-created by a device registration ("person zero" -- no human creator). Not
    /// <c>required</c>; see <see cref="PersonId"/> for why.
    /// </summary>
    [JsonIgnore]
    public int? CreatedByPersonId { get; init; }

    /// <summary>
    /// Gets a value indicating whether this person has the seed-only super-admin role. Never
    /// settable via any API -- only ever set directly in seed data. Not <c>required</c>; see
    /// <see cref="PersonId"/> for why.
    /// </summary>
    [JsonIgnore]
    public bool IsSuperAdmin { get; init; }

    /// <summary>
    /// Gets a value indicating whether the person's email address has been OTP-verified.
    /// </summary>
    public required bool IsEmailVerified { get; init; }

    /// <summary>
    /// Gets a value indicating whether the person's cell phone number has been OTP-verified.
    /// </summary>
    public required bool IsSmsVerified { get; init; }

    /// <summary>
    /// Gets the person-specific OTP verification window override, in minutes, or
    /// <see langword="null"/> to fall back to the global configured window.
    /// </summary>
    public required int? OtpWindowOverrideMinutes { get; init; }

    /// <summary>
    /// Gets the timestamp when the person record was inserted.
    /// </summary>
    public required DateTimeOffset InsertedOn { get; init; }

    /// <summary>
    /// Gets the timestamp when the person record was last updated.
    /// </summary>
    public required DateTimeOffset? UpdatedOn { get; init; }
}
