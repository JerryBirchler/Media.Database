using System.Text.Json.Serialization;

namespace Media.Database.Models
{
    /// <summary>
    /// Base response model for OTP verification.
    /// </summary>
    public record BaseOtpVerificationResponse
    {
        /// <summary>
        /// The unique identifier of the source machine. This is also the device's permanent API
        /// key (X-API-KEY) and is never serialized to a client. Not <c>required</c>:
        /// System.Text.Json refuses to build type metadata for a <c>required</c> member that is
        /// also <see cref="JsonIgnoreAttribute"/>-decorated, since JSON could never satisfy it.
        /// </summary>
        [property: JsonIgnore]
        public Guid SourceMachineUuid { get; set; } = Guid.Empty;

        /// <summary>
        /// The source machine name, as reported by the source machine itself. 
        /// This is not guaranteed to be unique, it may not be null or empty.
        /// </summary>
        [property: JsonPropertyName("sourceMachineName")]
        public required string SourceMachineName { get; set; } = string.Empty;

        /// <summary>
        /// The device type ID, as reported by the source machine itself. 
        /// This is not guaranteed to be unique, it may not be null or empty.
        /// </summary>
        [property: JsonPropertyName("deviceTypeId")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public required DeviceTypes DeviceTypeId { get; set; }

        /// <summary>
        /// The first name of the user associated with the source machine.
        /// This may not be null or empty.
        /// </summary>
        [property: JsonPropertyName("firstName")]
        public required string FirstName { get; set; } = string.Empty;

        /// <summary>
        /// The last name of the user associated with the source machine.
        /// This may not be null or empty.
        /// </summary>
        [property: JsonPropertyName("lastName")]
        public required string LastName { get; set; } = string.Empty;
    }
}
