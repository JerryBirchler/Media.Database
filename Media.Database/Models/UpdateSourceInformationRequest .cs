using System.Text.Json.Serialization;

namespace Media.Database.Models
{
    /// <summary>
    /// Request model for source machine information, used to register 
    /// a new source machine or update an existing one. All of the fields
    /// are required, and all but the email address, cell phone number, and 
    /// operating system make up a unique key for the source machine. The 
    /// operating system is included for informational purposes only.
    /// </summary>
    public record UpdateSourceInformationRequest
    {
        /// <summary>
        /// The source machine UUID as determined at the time of registration. 
        /// </summary>
        [property: JsonPropertyName("sourceMachineUuid")]
        public required Guid SourceMachineUuid { get; set; } = Guid.Empty;

        /// <summary>
        /// The email address of the user associated with the source machine. 
        /// This may not be null or empty and must be verified with a one-time
        /// password code that is sent to the email address.
        /// </summary>
        [property: JsonPropertyName("emailAddress")]
        public required string EmailAddress { get; set; } = string.Empty;

        /// <summary>
        /// The cell phone number of the user associated with the source machine.
        /// This may not be null or empty and must be verified with a one-time
        /// password code that is sent to the cell phone.
        /// </summary>
        [property: JsonPropertyName("cellPhoneNumber")]
        public required string CellPhoneNumber { get; set; } = string.Empty;

        /// <summary>
        /// <summary>
        /// The operating system of the source machine.
        /// This may not be null or empty. For example, 'Android', 'iOS', 'Windows', 'Linux', etc. 
        /// This is informational only and does not affect the uniqueness of the source machine.
        /// </summary>
        [property: JsonPropertyName("operatingSystem")]
        public required string OperatingSystem { get; set; } = string.Empty;
    }
}
