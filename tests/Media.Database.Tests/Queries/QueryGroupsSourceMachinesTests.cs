using Media.Database.Repositories.Queries;
using NUnit.Framework;
using Shouldly;

namespace Media.Database.Tests.Queries;

[TestFixture]
public class QueryGroupsSourceMachinesTests
{
    [Test]
    public void UpsertSql_Should_Contain_With_Updated_Inserted_UnionAll()
    {
        var sql = QueryGroupsSourceMachines.UpsertSql;
        sql.ShouldContain("WITH existing AS");
        sql.ShouldContain("updated AS");
        sql.ShouldContain("inserted AS");
        sql.ShouldContain("UNION ALL");
    }

    [Test]
    public void DeactivateSql_Should_Contain_Update_Where_Returning()
    {
        var sql = QueryGroupsSourceMachines.DeactivateSql;
        sql.ShouldContain("UPDATE");
        sql.ShouldContain("WHERE");
        sql.ShouldContain("RETURNING");
    }

    [Test]
    public void GetSourceMachineIdentifiersByGroupIdSql_Should_Contain_Select_Join_Where_OrderBy_Limit()
    {
        var sql = QueryGroupsSourceMachines.GetSourceMachineIdentifiersByGroupIdSql;
        sql.ShouldContain("SELECT");
        sql.ShouldContain("FROM");
        sql.ShouldContain("JOIN");
        sql.ShouldContain("WHERE");
        sql.ShouldContain("ORDER BY");
        sql.ShouldContain("@Limit");
        sql.ShouldContain("COALESCE");
    }

    [Test]
    public void GetSourceMachineIdentifiersByGroupIdSql_Should_Scope_To_A_Single_GroupId()
    {
        var sql = QueryGroupsSourceMachines.GetSourceMachineIdentifiersByGroupIdSql;
        sql.ShouldContain("\"GroupId\" = @GroupId");
    }

    [Test]
    public void GetSourceMachineIdentifiersByGroupIdSql_Should_OrderBy_SourceMachineName()
    {
        var sql = QueryGroupsSourceMachines.GetSourceMachineIdentifiersByGroupIdSql;
        sql.ShouldContain("\"SourceMachineName\" ASC");
    }

    [Test]
    public void GetSourceMachineIdentifiersByGroupIdSql_Should_Query_SourceMachineRegistrations()
    {
        // Regression test: must read from SourceMachineRegistrations (the table the existing
        // "registrations" Scylla table/CDC pipeline already mirrors), not just GroupsSourceMachines
        // -- so the pagination service can hydrate from that existing table without a new one.
        var sql = QueryGroupsSourceMachines.GetSourceMachineIdentifiersByGroupIdSql;
        sql.ShouldContain("\"SourceMachineRegistrations\"");
    }

    // ToGroupSourceMachine/ToSourceMachineIdentifier(s) (NpgsqlDataReader) are not unit-testable:
    // NpgsqlDataReader is sealed and cannot be mocked (see QueryFilesTests).
}
