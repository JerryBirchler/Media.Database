using Media.Database.Repositories.Queries;
using NUnit.Framework;
using Shouldly;

namespace Media.Database.Tests.Queries;

[TestFixture]
public class QueryFilesTests
{
    [Test]
    public void This_GetByIdSql_Should_Contain_Select_From_Where()
    {
        var sql = QueryFiles.GetByIdSql;
        sql.ShouldContain("SELECT");
        sql.ShouldContain("FROM");
        sql.ShouldContain("WHERE");
    }

    [Test]
    public void This_UpsertNoSql_Should_Contain_Insert_Values()
    {
        var sql = QueryFiles.UpsertNoSql;
        sql.ShouldContain("INSERT INTO");
        sql.ShouldContain("VALUES");
    }
}
