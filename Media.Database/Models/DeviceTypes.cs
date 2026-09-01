using System.Text.Json.Serialization;

namespace Media.Database.Models;

/// <summary>
/// Identifies which part of a file's metadata a word was extracted from.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum DeviceTypes
{
    /// <summary>
    /// The device is a tablet.
    /// </summary>
    Tablet,

    /// <summary>
    /// The device is a phone.
    /// </summary>
    Phone,

    /// <summary>
    /// The device is a PC.
    /// </summary>
    PC,

    /// <summary>
    /// The device is a FRAMEO device.
    /// </summary>
    Frameo
}
