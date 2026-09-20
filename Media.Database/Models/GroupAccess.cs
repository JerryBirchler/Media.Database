namespace Media.Database.Models;

/// <summary>
/// The result of resolving a <c>GroupPersonUuid</c> -- the multi-origin X-API-KEY model's third
/// credential type (MEDIA-34). Purely an internal auth-resolution DTO, not an API response --
/// unlike <see cref="SourceMachineRegistrations"/>, this never gets serialized to a client.
/// IsEmailVerified/IsSmsVerified reflect the person's own verification, not any device's, since a
/// GroupPersonUuid caller authenticates as themselves across every device in the group.
/// </summary>
public record GroupAccess
{
    public required int GroupId { get; init; }
    public required Guid GroupUuid { get; init; }
    public required string GroupName { get; init; }
    public required bool IsEmailVerified { get; init; }
    public required bool IsSmsVerified { get; init; }
}
