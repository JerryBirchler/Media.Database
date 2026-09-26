using Media.Database.Helpers;
using Media.Database.Models;
using Npgsql;
#pragma warning disable CS8981
using cd = Media.Database.Repositories.Schemas.TablesCql.DeviceSearchListsColumns;
using cg = Media.Database.Repositories.Schemas.TablesCql.GroupSearchListsColumns;
using cp = Media.Database.Repositories.Schemas.TablesCql.PersonSearchListsColumns;
using gv = Media.Database.Repositories.Schemas.TablesSql.GroupSearchListsColumns;
using os = Media.Database.Repositories.Schemas.OrdinalsSql;
using pn = Media.Database.Repositories.Schemas.ParameterNames;
using pv = Media.Database.Repositories.Schemas.TablesSql.PersonSearchListsColumns;
using tc = Media.Database.Repositories.Schemas.TablesCql;
using ts = Media.Database.Repositories.Schemas.TablesSql;
using vd = Media.Database.Repositories.Schemas.TablesSql.DeviceSearchListsColumns;
#pragma warning restore CS8981

namespace Media.Database.Repositories.Queries;

/// <summary>
/// What differs between the three scopes, and nothing else.
///
/// A device, a person and a group each own their lists in their own table pair, because mutually
/// exclusive nullable foreign keys push a rule that belongs in the schema out into every query.
/// The statements are otherwise identical, so they are written once and the identifiers come from
/// here -- the alternative was three of everything, which is how the second scope already looked
/// and how the third would have cemented it.
/// </summary>
internal sealed record SearchListScopeSchema
{
    /// <summary>The Postgres table, quoted.</summary>
    public required string Table { get; init; }

    /// <summary>The Scylla table.</summary>
    public required string CqlTable { get; init; }

    /// <summary>The identity column.</summary>
    public required string IdColumn { get; init; }

    /// <summary>The uuid column.</summary>
    public required string UuidColumn { get; init; }

    /// <summary>The owner column -- SourceMachineId, PersonId or GroupId.</summary>
    public required string OwnerColumn { get; init; }

    /// <summary>The list type column. From the registry per scope, not spelled as a literal.</summary>
    public required string ListTypeColumn { get; init; }

    /// <summary>The created column.</summary>
    public required string InsertedOnColumn { get; init; }

    /// <summary>The edited column.</summary>
    public required string UpdatedOnColumn { get; init; }

    /// <summary>The Scylla partition key column, which is the owner.</summary>
    public required string CqlOwnerColumn { get; init; }

    /// <summary>The Scylla clustering column, which is the borrowed identity.</summary>
    public required string CqlIdColumn { get; init; }

    /// <summary>The encrypted name column in Scylla.</summary>
    public required string CqlNameColumn { get; init; }

    /// <summary>The encrypted payload column in Scylla.</summary>
    public required string CqlPayloadColumn { get; init; }

    /// <summary>The payload version column in Scylla.</summary>
    public required string CqlPayloadVersionColumn { get; init; }

    /// <summary>The parameter carrying the owner.</summary>
    public required string OwnerParameter { get; init; }

    /// <summary>The parameter carrying the identity.</summary>
    public required string IdParameter { get; init; }

    /// <summary>The parameter carrying the uuid.</summary>
    public required string UuidParameter { get; init; }

    /// <summary>Maps a skeleton row, which needs this scope's own ordinal names.</summary>
    public required Func<NpgsqlDataReader, SearchList> Map { get; init; }

    /// <summary>Maps a content row.</summary>
    public required Func<Cassandra.Row, SearchListContent> MapContent { get; init; }

    /// <summary>The schema for a scope.</summary>
    public static SearchListScopeSchema For(OwnerScope scope) => scope switch
    {
        OwnerScope.Device => Device,
        OwnerScope.Person => Person,
        OwnerScope.Group => Group,
        _ => throw new ArgumentOutOfRangeException(nameof(scope), scope, "No search list table pair for this scope."),
    };

    private static readonly SearchListScopeSchema Device = new()
    {
        Table = ts.DeviceSearchLists,
        CqlTable = tc.DeviceSearchLists,
        IdColumn = vd.DeviceSearchListId,
        UuidColumn = vd.DeviceSearchListUuid,
        ListTypeColumn = vd.ListType,
        InsertedOnColumn = vd.InsertedOn,
        UpdatedOnColumn = vd.UpdatedOn,
        OwnerColumn = vd.SourceMachineId,
        CqlOwnerColumn = cd.SourceMachineId,
        CqlNameColumn = cd.Name,
        CqlPayloadColumn = cd.Payload,
        CqlPayloadVersionColumn = cd.PayloadVersion,
        CqlIdColumn = cd.DeviceSearchListId,
        OwnerParameter = pn.SourceMachineId,
        IdParameter = pn.DeviceSearchListId,
        UuidParameter = pn.DeviceSearchListUuid,
        Map = reader => Skeleton(reader, OwnerScope.Device, os.DeviceSearchListId, os.DeviceSearchListUuid, os.SourceMachineId),
        MapContent = row => Content(row, cd.DeviceSearchListId, cd.Name, cd.Payload, cd.PayloadVersion),
    };

    private static readonly SearchListScopeSchema Person = new()
    {
        Table = ts.PersonSearchLists,
        CqlTable = tc.PersonSearchLists,
        IdColumn = pv.PersonSearchListId,
        UuidColumn = pv.PersonSearchListUuid,
        ListTypeColumn = pv.ListType,
        InsertedOnColumn = pv.InsertedOn,
        UpdatedOnColumn = pv.UpdatedOn,
        OwnerColumn = pv.PersonId,
        CqlOwnerColumn = cp.PersonId,
        CqlNameColumn = cp.Name,
        CqlPayloadColumn = cp.Payload,
        CqlPayloadVersionColumn = cp.PayloadVersion,
        CqlIdColumn = cp.PersonSearchListId,
        OwnerParameter = pn.PersonId,
        IdParameter = pn.PersonSearchListId,
        UuidParameter = pn.PersonSearchListUuid,
        Map = reader => Skeleton(reader, OwnerScope.Person, os.PersonSearchListId, os.PersonSearchListUuid, os.PersonId),
        MapContent = row => Content(row, cp.PersonSearchListId, cp.Name, cp.Payload, cp.PayloadVersion),
    };

    private static readonly SearchListScopeSchema Group = new()
    {
        Table = ts.GroupSearchLists,
        CqlTable = tc.GroupSearchLists,
        IdColumn = gv.GroupSearchListId,
        UuidColumn = gv.GroupSearchListUuid,
        ListTypeColumn = gv.ListType,
        InsertedOnColumn = gv.InsertedOn,
        UpdatedOnColumn = gv.UpdatedOn,
        OwnerColumn = gv.GroupId,
        CqlOwnerColumn = cg.GroupId,
        CqlNameColumn = cg.Name,
        CqlPayloadColumn = cg.Payload,
        CqlPayloadVersionColumn = cg.PayloadVersion,
        CqlIdColumn = cg.GroupSearchListId,
        OwnerParameter = pn.GroupId,
        IdParameter = pn.GroupSearchListId,
        UuidParameter = pn.GroupSearchListUuid,
        Map = reader => Skeleton(reader, OwnerScope.Group, os.GroupSearchListId, os.GroupSearchListUuid, os.GroupId),
        MapContent = row => Content(row, cg.GroupSearchListId, cg.Name, cg.Payload, cg.PayloadVersion),
    };

    /// <summary>
    /// The name and the lines are absent by design -- they live in Scylla, and this half of the
    /// list has deliberately never seen them.
    /// </summary>
    private static SearchList Skeleton(NpgsqlDataReader reader, OwnerScope scope, string id, string uuid, string owner) =>
        new()
        {
            Id = reader.GetInt32(id),
            Uuid = reader.GetGuid(uuid),
            Scope = scope,
            OwnerId = reader.GetInt32(owner),
            ListType = (SearchListType)reader.GetInt32(os.ListType),
            InsertedOn = reader.GetFieldValue<DateTimeOffset>(os.InsertedOn),
            UpdatedOn = reader.GetFieldValue<DateTimeOffset?>(os.UpdatedOn),
        };

    private static SearchListContent Content(Cassandra.Row row, string id, string name, string payload, string version) =>
        new()
        {
            Id = row.GetValue<int>(id),
            Name = row.GetValue<string>(name),
            Payload = row.GetValue<string>(payload),
            PayloadVersion = row.GetValue<int>(version),
        };
}
