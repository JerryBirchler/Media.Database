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
    }
}
