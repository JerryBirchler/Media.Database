using System.Text.Json.Serialization;

namespace Media.Database.Models;

/// <summary>
/// What a saved search list holds. Both kinds share a row and a payload column; this says how to
/// read the payload.
///
/// Values are explicit because they are persisted: <c>ListType</c> holds the number and the
/// <c>SearchListTypes</c> table seeds these exact ids, so reordering the members would silently
/// reinterpret every stored row -- the same reasoning as <see cref="WordType"/>.
///
/// Not a boolean, deliberately. A further kind of list costs a member here and a seed row rather
/// than a migration.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SearchListType
{
    /// <summary>Unset. The default, so a row that was never given a type reads as wrong rather than as an OR list.</summary>
    None = 0,

    /// <summary>Lines that OR together. The payload holds the lines.</summary>
    Or = 1,

    /// <summary>References to other lists, which AND together. The payload holds their uuids.</summary>
    And = 2,
}
