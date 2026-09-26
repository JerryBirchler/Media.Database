using System.Text.Json.Serialization;

namespace Media.Database.Models;

/// <summary>
/// Who owns a resource: the device it was made on, the person who owns that device, or the group
/// they belong to.
///
/// In the order things come into existence. A device exists from registration; a person once
/// enrolled; a group once joined. Later scopes are elaborations, and no base feature may require
/// one -- see the invariant in Media.Api/CLAUDE.md.
///
/// Deliberately general rather than named for saved search lists, which are merely the first
/// thing to need it: the person-or-group distinction recurs, and so does the authorization
/// rule that goes with it (see AuthenticatedOwnerAttribute).
///
/// The two scopes are separate tables rather than one table with nullable <c>PersonId</c> and
/// <c>GroupId</c>: mutually exclusive nullable foreign keys push a rule that belongs in the schema
/// out into every query and every reader's head. This enum is how the single API route maps onto
/// that split, which is a presentation concern and muddles no stored row.
///
/// Not persisted -- it is implied by the table a row lives in -- so the values stay implicit.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum OwnerScope
{
    /// <summary>
    /// The device itself. The base scope: a device exists from registration, before anyone has
    /// enrolled and before any group, and nothing in the base path may require what does not
    /// exist yet. Its resources go with it, so the foreign key cascades.
    /// </summary>
    Device,

    /// <summary>Private to one person. Their lists go with them, so the foreign key cascades.</summary>
    Person,

    /// <summary>Shared vocabulary within a group. Deleting the group is restricted, not cascaded.</summary>
    Group,
}
