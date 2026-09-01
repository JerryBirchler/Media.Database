using System.Text.Json.Serialization;

namespace Media.Database.Models
{
    public record AddRegistrationResponse
    {
        public required int Id { get; set; }
        public required string OtpEmail { get; set; } = string.Empty;
        public required string OtpCellPhone { get; set; } = string.Empty;
        public required bool IsEmailVerified { get; set; } = false;
        public required bool IsSmsVerified { get; set; } = false;
        public required DateTimeOffset InsertedOn { get; set; }
        public required DateTimeOffset? UpdatedOn { get; set; }
    }
}
