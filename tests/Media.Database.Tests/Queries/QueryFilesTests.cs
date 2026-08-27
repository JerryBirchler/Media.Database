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
    public void This_UpsertCql_Should_Contain_Insert_Values()
    {
        var sql = QueryFiles.UpsertCql;
        sql.ShouldContain("INSERT INTO");
        sql.ShouldContain("VALUES");
    }

    [Test]
    public void GetHistoryPagesBySourceMachineIdSql_Should_Contain_Select_Where_OrderBy_Limit()
    {
        var sql = QueryFiles.GetHistoryPagesBySourceMachineIdSql;
        sql.ShouldContain("SELECT");
        sql.ShouldContain("FROM");
        sql.ShouldContain("WHERE");
        sql.ShouldContain("ORDER BY");
        sql.ShouldContain("@Limit");
    }

    [Test]
    public void GetCurrentBySourceMachineIdSql_Should_Contain_Select_From_Where_Limit1()
    {
        var sql = QueryFiles.GetCurrentBySourceMachineIdSql;
        sql.ShouldContain("SELECT");
        sql.ShouldContain("FROM");
        sql.ShouldContain("WHERE");
        sql.ShouldContain("LIMIT 1");
    }

    [Test]
    public void GetCurrentPagesBySourceMachineIdSql_Should_Contain_Select_Where_OrderBy_Limit()
    {
        var sql = QueryFiles.GetCurrentPagesBySourceMachineIdSql;
        sql.ShouldContain("SELECT");
        sql.ShouldContain("FROM");
        sql.ShouldContain("WHERE");
        sql.ShouldContain("ORDER BY");
        sql.ShouldContain("@Limit");
        sql.ShouldContain("COALESCE");
    }

    [Test]
    public void GetPreviousIdsSql_Should_Contain_Update_Where_Returning()
    {
        var sql = QueryFiles.GetPreviousIdsSql;
        sql.ShouldContain("UPDATE");
        sql.ShouldContain("WHERE");
        sql.ShouldContain("RETURNING");
    }

    [Test]
    public void UpsertSql_Should_Contain_Insert_OnConflict_DoUpdate()
    {
        var sql = QueryFiles.UpsertSql;
        sql.ShouldContain("INSERT INTO");
        sql.ShouldContain("VALUES");
        sql.ShouldContain("ON CONFLICT");
        sql.ShouldContain("DO UPDATE SET");
        sql.ShouldContain("RETURNING *");
    }

    [Test]
    public void UpdateSql_Should_Contain_Update_Where_Returning()
    {
        var sql = QueryFiles.UpdateSql;
        sql.ShouldContain("UPDATE");
        sql.ShouldContain("WHERE");
        sql.ShouldContain("RETURNING *");
    }

    [Test]
    public void ExistsSql_Should_Contain_Select_Where_Limit1()
    {
        var sql = QueryFiles.ExistsSql;
        sql.ShouldContain("SELECT");
        sql.ShouldContain("FROM");
        sql.ShouldContain("WHERE");
        sql.ShouldContain("LIMIT 1");
    }

    [Test]
    public void DeleteSql_Should_Contain_Delete_Where_Returning()
    {
        var sql = QueryFiles.DeleteSql;
        sql.ShouldContain("DELETE FROM");
        sql.ShouldContain("WHERE");
        sql.ShouldContain("RETURNING 1");
    }

    [Test]
    public void DeleteHistorySql_Should_Contain_Delete_Where_Returning_OrderBy()
    {
        var sql = QueryFiles.DeleteHistorySql;
        sql.ShouldContain("DELETE FROM");
        sql.ShouldContain("WHERE");
        sql.ShouldContain("RETURNING *");
        sql.ShouldContain("ORDER BY");
    }

    [Test]
    public void GetByIdCql_Should_Contain_Select_From_Where_Limit1()
    {
        var sql = QueryFiles.GetByIdCql;
        sql.ShouldContain("SELECT");
        sql.ShouldContain("FROM");
        sql.ShouldContain("WHERE");
        sql.ShouldContain("LIMIT 1");
    }

    [Test]
    public void InactivateCql_Should_Contain_Update_Where()
    {
        var sql = QueryFiles.InactivateCql;
        sql.ShouldContain("UPDATE");
        sql.ShouldContain("WHERE");
    }

    [Test]
    public void UpdateCql_Should_Contain_Update_Where()
    {
        var sql = QueryFiles.UpdateCql;
        sql.ShouldContain("UPDATE");
        sql.ShouldContain("WHERE");
    }

    [Test]
    public void DeleteCql_Should_Contain_Delete_From_Where()
    {
        var sql = QueryFiles.DeleteCql;
        sql.ShouldContain("DELETE FROM");
        sql.ShouldContain("WHERE");
    }

    // ToFile/ToFiles/ToId/ToIds (NpgsqlDataReader) and Row.ToFile (Cassandra) are not
    // unit-testable: NpgsqlDataReader is sealed (cannot be mocked), and Row.ToFile transitively
    // calls Row.IsNull via GetValueOrDefault<T>, which is non-overridable and cannot be stubbed
    // by Moq. Exercising these mapping methods requires a real reader/row (integration-test territory).
}
