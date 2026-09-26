using System.Text.Json.Serialization;

namespace Media.Database.Models;

/// <summary>
/// Whether a resource is owned by one person or shared by a group.
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
    /// <summary>Private to one person. Their lists go with them, so the foreign key cascades.</summary>
    Person,

    /// <summary>Shared vocabulary within a group. Deleting the group is restricted, not cascaded.</summary>
    Group,
}
