using Media.Database.Repositories.Queries;
using NUnit.Framework;
using Shouldly;

namespace Media.Database.Tests.Queries;

[TestFixture]
public class QueryWordsTests
{
    [Test]
    public void GetByIdSql_Should_Contain_Select_From_Where()
    {
        var sql = QueryWords.GetByIdSql;
        sql.ShouldContain("SELECT");
        sql.ShouldContain("FROM");
        sql.ShouldContain("WHERE");
        sql.ShouldContain("LIMIT 1");
    }

    [Test]
    public void SelectFilePages_Should_Contain_Select_From()
    {
        var sql = QueryWords.SelectFilePages;
        sql.ShouldContain("SELECT");
        sql.ShouldContain("FROM");
    }

    [Test]
    public void AndFilePages_Should_Contain_And_Conditions()
    {
        var sql = QueryWords.AndFilePages;
        sql.ShouldContain("AND");
        sql.ShouldContain("IS NULL");
        sql.ShouldContain("OR");
    }

    [Test]
    public void GetFilePagesByWordOriginSql_Should_Contain_Select_Where_OrderBy_Limit()
    {
        var sql = QueryWords.GetFilePagesByWordOriginSql;
        sql.ShouldContain("SELECT");
        sql.ShouldContain("FROM");
        sql.ShouldContain("WHERE");
        sql.ShouldContain("ORDER BY");
        sql.ShouldContain("LIMIT");
        sql.ShouldContain("COALESCE");
    }

    [Test]
    public void GetFilePagesByWordFileIdSql_Should_Contain_Select_Where_OrderBy_Limit()
    {
        var sql = QueryWords.GetFilePagesByWordFileIdSql;
        sql.ShouldContain("SELECT");
        sql.ShouldContain("FROM");
        sql.ShouldContain("WHERE");
        sql.ShouldContain("ORDER BY");
        sql.ShouldContain("LIMIT");
        sql.ShouldContain("COALESCE");
    }

    [Test]
    public void GetFilePagesByFileIdWordSql_Should_Contain_Select_Where_OrderBy_Limit()
    {
        var sql = QueryWords.GetFilePagesByFileIdWordSql;
        sql.ShouldContain("SELECT");
        sql.ShouldContain("FROM");
        sql.ShouldContain("WHERE");
        sql.ShouldContain("ORDER BY");
        sql.ShouldContain("LIMIT");
        sql.ShouldContain("COALESCE");
    }

    [Test]
    public void GetFilePagesByFileIdOriginSql_Should_Contain_Select_Where_OrderBy_Limit()
    {
        var sql = QueryWords.GetFilePagesByFileIdOriginSql;
        sql.ShouldContain("SELECT");
        sql.ShouldContain("FROM");
        sql.ShouldContain("WHERE");
        sql.ShouldContain("ORDER BY");
        sql.ShouldContain("LIMIT");
        sql.ShouldContain("COALESCE");
    }

    [Test]
    public void UpsertWordSql_Should_Contain_Insert_Values_OnConflict()
    {
        var sql = QueryWords.UpsertWordSql;
        sql.ShouldContain("INSERT INTO");
        sql.ShouldContain("VALUES");
        sql.ShouldContain("ON CONFLICT");
        sql.ShouldContain("DO UPDATE SET");
        sql.ShouldContain("DO NOTHING");
        sql.ShouldContain("WITH inserted_rows AS");
    }

    [Test]
    public void RefreshViewSql_Should_Contain_RefreshMaterializedView()
    {
        var sql = QueryWords.RefreshViewSql;
        sql.ShouldContain("REFRESH MATERIALIZED VIEW");
        sql.ShouldContain("CONCURRENTLY");
    }

    [Test]
    public void DeleteFileSql_Should_Contain_Delete_From_Where()
    {
        var sql = QueryWords.DeleteFileSql;
        sql.ShouldContain("DELETE FROM");
        sql.ShouldContain("WHERE");
    }

    [Test]
    public void GetByIdSql_Should_Select_All_Word_Columns()
    {
        var sql = QueryWords.GetByIdSql;
        sql.ShouldContain("Id", Case.Insensitive);
        sql.ShouldContain("Word", Case.Insensitive);
        sql.ShouldContain("Origin", Case.Insensitive);
        sql.ShouldContain("IsProperName", Case.Insensitive);
        sql.ShouldContain("InsertedOn", Case.Insensitive);
        sql.ShouldContain("UpdatedOn", Case.Insensitive);
        sql.ShouldContain("CameFromFileId", Case.Insensitive);
    }

    [Test]
    public void SelectFilePages_Should_Select_All_ViewWordFiles_Columns()
    {
        var sql = QueryWords.SelectFilePages;
        sql.ShouldContain("Origin", Case.Insensitive);
        sql.ShouldContain("WordId", Case.Insensitive);
        sql.ShouldContain("Word", Case.Insensitive);
        sql.ShouldContain("FileId", Case.Insensitive);
        sql.ShouldContain("IsCurrent", Case.Insensitive);
        sql.ShouldContain("IsProperName", Case.Insensitive);
    }

    [Test]
    public void GetFilePagesByWordOriginSql_Should_OrderBy_IsCurrent_Word_Origin_FileId()
    {
        var sql = QueryWords.GetFilePagesByWordOriginSql;
        var orderByIndex = sql.IndexOf("ORDER BY");
        var limitIndex = sql.IndexOf("LIMIT");

        orderByIndex.ShouldBeGreaterThan(0);
        limitIndex.ShouldBeGreaterThan(orderByIndex);

        var orderByClause = sql.Substring(orderByIndex, limitIndex - orderByIndex);
        orderByClause.ShouldContain("IsCurrent", Case.Insensitive);
        orderByClause.ShouldContain("Word", Case.Insensitive);
        orderByClause.ShouldContain("Origin", Case.Insensitive);
        orderByClause.ShouldContain("FileId", Case.Insensitive);
    }

    [Test]
    public void GetFilePagesByWordFileIdSql_Should_OrderBy_IsCurrent_Word_FileId_Origin()
    {
        var sql = QueryWords.GetFilePagesByWordFileIdSql;
        var orderByIndex = sql.IndexOf("ORDER BY");
        var limitIndex = sql.IndexOf("LIMIT");

        orderByIndex.ShouldBeGreaterThan(0);
        limitIndex.ShouldBeGreaterThan(orderByIndex);

        var orderByClause = sql.Substring(orderByIndex, limitIndex - orderByIndex);
        orderByClause.ShouldContain("IsCurrent", Case.Insensitive);
        orderByClause.ShouldContain("Word", Case.Insensitive);
        orderByClause.ShouldContain("FileId", Case.Insensitive);
        orderByClause.ShouldContain("Origin", Case.Insensitive);
    }

    [Test]
    public void GetFilePagesByFileIdWordSql_Should_OrderBy_IsCurrent_FileId_Word_Origin()
    {
        var sql = QueryWords.GetFilePagesByFileIdWordSql;
        var orderByIndex = sql.IndexOf("ORDER BY");
        var limitIndex = sql.IndexOf("LIMIT");

        orderByIndex.ShouldBeGreaterThan(0);
        limitIndex.ShouldBeGreaterThan(orderByIndex);

        var orderByClause = sql.Substring(orderByIndex, limitIndex - orderByIndex);
        orderByClause.ShouldContain("IsCurrent", Case.Insensitive);
        orderByClause.ShouldContain("FileId", Case.Insensitive);
        orderByClause.ShouldContain("Word", Case.Insensitive);
        orderByClause.ShouldContain("Origin", Case.Insensitive);
    }

    [Test]
    public void GetFilePagesByFileIdOriginSql_Should_OrderBy_IsCurrent_FileId_Origin_Word()
    {
        var sql = QueryWords.GetFilePagesByFileIdOriginSql;
        var orderByIndex = sql.IndexOf("ORDER BY");
        var limitIndex = sql.IndexOf("LIMIT");

        orderByIndex.ShouldBeGreaterThan(0);
        limitIndex.ShouldBeGreaterThan(orderByIndex);

        var orderByClause = sql.Substring(orderByIndex, limitIndex - orderByIndex);
        orderByClause.ShouldContain("IsCurrent", Case.Insensitive);
        orderByClause.ShouldContain("FileId", Case.Insensitive);
        orderByClause.ShouldContain("Origin", Case.Insensitive);
        orderByClause.ShouldContain("Word", Case.Insensitive);
    }

    [Test]
    public void UpsertWordSql_Should_Insert_All_Required_Word_Fields()
    {
        var sql = QueryWords.UpsertWordSql;
        sql.ShouldContain("@Word", Case.Insensitive);
        sql.ShouldContain("@Origin", Case.Insensitive);
        sql.ShouldContain("@IsProperName", Case.Insensitive);
        sql.ShouldContain("@UpdatedOn", Case.Insensitive);
        sql.ShouldContain("@CameFromFileId", Case.Insensitive);
    }

    [Test]
    public void UpsertWordSql_Should_Handle_Conflict_On_Word()
    {
        var sql = QueryWords.UpsertWordSql;
        sql.ShouldContain("ON CONFLICT", Case.Insensitive);
        sql.ShouldContain("\"Word\"", Case.Sensitive);
    }

    [Test]
    public void UpsertWordSql_Should_Insert_Into_WordFiles_Table()
    {
        var sql = QueryWords.UpsertWordSql;
        var firstInsert = sql.IndexOf("INSERT INTO");
        var secondInsert = sql.IndexOf("INSERT INTO", firstInsert + 1);

        secondInsert.ShouldBeGreaterThan(firstInsert);
    }

    [Test]
    public void DeleteFileSql_Should_Reference_FileId_Parameter()
    {
        var sql = QueryWords.DeleteFileSql;
        sql.ShouldContain("@FileId", Case.Insensitive);
    }

    [Test]
    public void AndFilePages_Should_Handle_Nullable_IsCurrent_Parameter()
    {
        var sql = QueryWords.AndFilePages;
        sql.ShouldContain("@IsCurrent", Case.Insensitive);
        sql.ShouldContain("IS NULL");
    }

    [Test]
    public void AndFilePages_Should_Handle_Nullable_IsProperName_Parameter()
    {
        var sql = QueryWords.AndFilePages;
        sql.ShouldContain("@IsProperName", Case.Insensitive);
        sql.ShouldContain("IS NULL");
    }
}
