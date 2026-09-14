using Media.Database.Repositories.Queries;
using NUnit.Framework;
using Shouldly;

namespace Media.Database.Tests.Queries;

[TestFixture]
public class QueryPersonsSourceMachinesTests
{
    [Test]
    public void UpsertSql_Should_Contain_With_Updated_Inserted_UnionAll()
    {
        var sql = QueryPersonsSourceMachines.UpsertSql;
        sql.ShouldContain("WITH existing AS");
        sql.ShouldContain("updated AS");
        sql.ShouldContain("inserted AS");
        sql.ShouldContain("UNION ALL");
    }

    [Test]
    public void GetActiveSql_Should_Contain_Select_From_Where()
    {
        var sql = QueryPersonsSourceMachines.GetActiveSql;
        sql.ShouldContain("SELECT");
        sql.ShouldContain("FROM");
        sql.ShouldContain("WHERE");
    }

    [Test]
    public void ListActiveByPersonSql_Should_Contain_Select_From_Where()
    {
        var sql = QueryPersonsSourceMachines.ListActiveByPersonSql;
        sql.ShouldContain("SELECT");
        sql.ShouldContain("FROM");
        sql.ShouldContain("WHERE");
    }

    [Test]
    public void AddSql_Should_Contain_Insert_Values_Returning()
    {
        var sql = QueryPersonsSourceMachines.AddSql;
        sql.ShouldContain("INSERT INTO");
        sql.ShouldContain("VALUES");
        sql.ShouldContain("RETURNING");
    }
}
