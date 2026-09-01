using System.Text.Json.Serialization;

namespace Media.Database.Models
{
    /// <summary>
    /// Response model for OTP cell phone verification, used to indicate whether
    /// the cell phone number of the user associated with the source machine has
    /// been verified with a one-time password code. All of the fields
    /// are required.
    /// </summary>
    public record OtpSmsResponse : BaseOtpVerificationResponse
    {
        /// <summary>
        /// The cell phone number of the user associated with the source machine. 
        /// This may not be null or empty and must be verified with a one-time
        /// password code that is sent to the cell phone number.
        /// </summary>
        [property: JsonPropertyName("cellPhoneNumber")]
        public required string CellPhoneNumber { get; init; } = string.Empty;

        /// <summary>
        /// Indicates whether the cell phone number has been verified with a one-time
        /// password code that is sent to the cell phone number.
        /// </summary>
        [property: JsonPropertyName("otpSmsVerified")]
        public required bool OtpSmsVerified { get; init; } = false;
    }
}
