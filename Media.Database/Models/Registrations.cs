using System.Text.Json.Serialization;

namespace Media.Database.Models;

/// <summary>
/// Represents a registration of a machine that originates files tracked by the media database.
/// </summary>
public class Registrations
{
    /// <summary>
    /// Gets or sets the integer identifier for the registration.
    /// </summary>
    [JsonPropertyName("id")]
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the integer identifier for the source machine.
    /// </summary>
    [JsonPropertyName("sourceMachineId")]
    public required int SourceMachineId { get; set; }

    /// <summary>
    /// Gets or sets the email address of the source machine owner.
    /// </summary>
    [JsonPropertyName("emailAddress")]
    public required string EmailAddress { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the one-time password (OTP) for the email address of the source machine owner.
    /// </summary>
    [JsonPropertyName("otpEmail")]
    public required string OtpEmail { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the cell phone number of the source machine owner.
    /// </summary>
    [JsonPropertyName("cellPhoneNumber")]
    public required string CellPhoneNumber { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the one-time password (OTP) for the cell phone number of the source machine owner.
    /// </summary>
    [JsonPropertyName("otpCellPhone")]
    public required string OtpCellPhone { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the registration's email address has been verified.
    /// </summary>
    [JsonPropertyName("isEmailVerified")]
    public required bool IsEmailVerified { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether the registration's cell phone number has been verified.
    /// </summary>
    [JsonPropertyName("isSmsVerified")]
    public required bool IsSmsVerified { get; set; } = false;

    /// <summary>
    /// Gets or sets the timestamp when the registration record was inserted.
    /// </summary>
    [JsonPropertyName("insertedOn")]
    public DateTimeOffset InsertedOn { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the registration record was last updated.
    /// </summary>
    [JsonPropertyName("updatedOn")]
    public DateTimeOffset? UpdatedOn { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the registration is current.
    /// </summary>
    [JsonPropertyName("isCurrent")]
    public bool IsCurrent { get; set; } = false;
}
