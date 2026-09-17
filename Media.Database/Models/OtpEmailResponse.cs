using System.Text.Json.Serialization;

namespace Media.Database.Models
{
    /// <summary>
    /// Response model for OTP email verification, used to indicate whether
    /// the email address of the user associated with the source machine has
    /// been verified with a one-time password code. All of the fields
    /// are required.
    /// </summary>
    public record OtpEmailResponse : BaseOtpVerificationResponse
    {
        /// <summary>
        /// The email address of the user associated with the source machine. 
        /// This may not be null or empty and must be verified with a one-time
        /// password code that is sent to the email address.
        /// </summary>
        [property: JsonPropertyName("emailAddress")]
        public required string EmailAddress { get; init; } = string.Empty;

        /// <summary>
        /// The cell phone number on file, or empty if none has been submitted yet -- the signal a
        /// client uses to tell "still needs a phone number" apart from "already has one, needs its
        /// own OTP verified".
        /// </summary>
        [property: JsonPropertyName("cellPhoneNumber")]
        public required string CellPhoneNumber { get; init; } = string.Empty;

        /// <summary>
        /// Indicates whether the email address has been verified with a one-time
        /// password code that is sent to the email address.
        /// </summary>
        [property: JsonPropertyName("otpEmailVerified")]
        public required bool OtpEmailVerified { get; init; } = false;
    }
}
