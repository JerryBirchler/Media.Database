using System.Text.Json.Serialization;

namespace Media.Database.Models
{
    /// <summary>
    /// Result of a resend operation: which channel(s), if any, received a freshly generated OTP.
    /// A channel that is already verified is never resent.
    /// </summary>
    public record ResendOtpResult
    {
        /// <summary>
        /// Whether a new email OTP was generated and needs to be sent.
        /// </summary>
        public required bool EmailOtpSent { get; set; }

        /// <summary>
        /// Whether a new SMS OTP was generated and needs to be sent.
        /// </summary>
        public required bool SmsOtpSent { get; set; }

        /// <summary>
        /// The freshly generated email OTP code, or empty if the email channel was already
        /// verified and nothing was regenerated. Never serialized to a client. Not <c>required</c>:
        /// System.Text.Json refuses to build type metadata for a <c>required</c> member that is
        /// also <see cref="JsonIgnoreAttribute"/>-decorated, since JSON could never satisfy it.
        /// </summary>
        [JsonIgnore]
        public string OtpEmail { get; set; } = string.Empty;
    }
}
