namespace Media.Database.Models;

/// <summary>
/// A device someone owns whose <see cref="GroupShell"/> has not yet been made into a group
/// (MEDIA-37): what they can turn into a group of their own. Carries the shell's id as the handle
/// to promote it by -- an internal number, not a credential -- and never the device's uuid.
/// </summary>
public record UnpromotedShell
{
    /// <summary>Gets the shell's identifier: what the person chooses when creating their group.</summary>
    public required int GroupShellId { get; init; }

    /// <summary>Gets the device the shell came from.</summary>
    public required int SourceMachineId { get; init; }

    /// <summary>Gets the device's name, as the person registered it.</summary>
    public required string SourceMachineName { get; init; }

    /// <summary>Gets the kind of device.</summary>
    public required DeviceTypes DeviceTypeId { get; init; }

    /// <summary>Gets the device's operating system, when known.</summary>
    public string? OperatingSystem { get; init; }

    /// <summary>Gets when the shell was made, which is when the device registered.</summary>
    public required DateTimeOffset InsertedOn { get; init; }
}
