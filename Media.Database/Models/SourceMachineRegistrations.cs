using System.Text.Json.Serialization;

namespace Media.Database.Models;

/// <summary>
/// Represents a machine that originates files tracked by the media database.
/// </summary>
public record SourceMachineRegistrations
{
    /// <summary>
    /// Gets the integer identifier for the registration.
    /// </summary>
    [JsonIgnore]
    public required int RegistrationId { get; set; }
    /// <summary>
    /// Gets the integer identifier for the source machine.
    /// </summary>
    [JsonIgnore]
    public required int SourceMachineId { get; init; }

    /// <summary>
    /// Gets the unique identifier for the source machine.
    /// </summary>
    [JsonPropertyName("sourceMachineUuid")]
    public required Guid SourceMachineUuid { get; init; }

    /// <summary>
    /// Gets the name of the source machine.
    /// </summary>
    [JsonPropertyName("sourceMachineName")]
    public required string SourceMachineName { get; init; } = string.Empty;
    
    /// <summary>
    /// Gets the device type identifier for the source machine.
    /// </summary>
    [JsonPropertyName("deviceTypeId")]
    public required DeviceTypes DeviceTypeId { get; init; }

    /// <summary>
    /// Gets the operating system of the source machine.
    /// </summary>
    [JsonPropertyName("operatingSystem")]
    public required string OperatingSystem { get; init; } = string.Empty;

    /// <summary>
    /// Gets the first name of the source machine owner.
    /// </summary>
    [JsonPropertyName("firstName")]
    public required string FirstName { get; init; } = string.Empty;

    /// <summary>
    /// Gets the last name of the source machine owner.
    /// </summary>  
    [JsonPropertyName("lastName")]
    public required string LastName { get; init; } = string.Empty;

    /// <summary>
    /// Gets the email address of the source machine owner.
    /// </summary>
    [JsonPropertyName("emailAddress")]
    public required string EmailAddress { get; init; } = string.Empty;

    /// <summary>
    /// Gets the cell phone number of the source machine owner.
    /// </summary>
    [JsonPropertyName("cellPhoneNumber")]
    public required string CellPhoneNumber { get; init; } = string.Empty;

    /// <summary>
    /// Initialized to true if the current registration matches both the 
    /// email address and the cell phone number.
    /// </summary>
    [JsonPropertyName("hasRegistration")]
    public required bool HasRegistration { get; init; }

    /// <summary>
    /// Gets a value indicating whether the source machine's email address has been verified.
    /// </summary>
    [JsonPropertyName("isEmailVerified")]
    public required bool IsEmailVerified { get; init; } = false;
    /// <summary>
    /// Gets a value indicating whether the source machine's cell phone number has been verified.
    /// </summary>
    [JsonPropertyName("isSmsVerified")]
    public required bool IsSmsVerified { get; init; } = false;

    /// <summary>
    /// Gets the timestamp when the source machine record was inserted.
    /// </summary>
    [JsonPropertyName("insertedOn")]
    public required DateTimeOffset InsertedOn { get; init; }

    /// <summary>
    /// Gets the timestamp when the source machine record was last updated.
    /// </summary>
    [JsonPropertyName("updatedOn")]
    public required DateTimeOffset? UpdatedOn { get; init; }

    /// <summary>
    /// Gets a value indicating whether the source machine is active.
    /// </summary>
    [JsonPropertyName("isActive")]
    public required bool IsActive { get; init; } = false;

    /// <summary>
    /// Gets the one-time password (OTP) email address for the source machine owner.
    /// </summary>
    [JsonIgnore]
    public required string OtpEmail { get; set; }
    
    /// <summary>
    /// Gets the one-time password (OTP) cell phone number for the source machine owner.
    /// </summary>  
    [JsonIgnore]
    public required string OtpCellPhone { get; set; }

    /// <summary>
    /// Gets the timestamp when the registration record was inserted.
    /// </summary>
    [JsonIgnore]
    public required DateTimeOffset? RegistrationInsertedOn { get; set; }

    /// <summary>
    /// Gets the timestamp when the registration record was last updated.
    /// </summary>
    [JsonIgnore] 
    public required DateTimeOffset? RegistrationUpdatedOn { get; set; }
}
