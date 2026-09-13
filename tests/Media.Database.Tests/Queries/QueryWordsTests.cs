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
    public void AndFilePages_Should_Contain_And_Conditions()
    {
        var sql = QueryWords.AndFilePages;
        sql.ShouldContain("AND");
        sql.ShouldContain("IS NULL");
        sql.ShouldContain("OR");
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
    public void SelectFilePagesWithSourceMachineId_Should_Select_SourceMachineId_And_Everything_Else()
    {
        var sql = QueryWords.SelectFilePagesWithSourceMachineId;
        sql.ShouldContain("SourceMachineId");
        sql.ShouldContain("Origin", Case.Insensitive);
        sql.ShouldContain("WordId", Case.Insensitive);
        sql.ShouldContain("Word", Case.Insensitive);
        sql.ShouldContain("FileId", Case.Insensitive);
        sql.ShouldContain("IsCurrent", Case.Insensitive);
        sql.ShouldContain("IsProperName", Case.Insensitive);
    }

    [TestCase(nameof(QueryWords.GetFileIdentifiersByWordOriginSql))]
    [TestCase(nameof(QueryWords.GetFileIdentifiersByWordFileIdSql))]
    [TestCase(nameof(QueryWords.GetFileIdentifiersByFileIdWordSql))]
    [TestCase(nameof(QueryWords.GetFileIdentifiersByFileIdOriginSql))]
    [TestCase(nameof(QueryWords.GetFileIdentifiersByFilePathOriginSql))]
    [TestCase(nameof(QueryWords.GetFileIdentifiersByFilePathWordSql))]
    [TestCase(nameof(QueryWords.GetFileIdentifiersByWordFilePathSql))]
    public void GeneralFileIdentifierQueries_Should_Not_Select_SourceMachineId(string propertyName)
    {
        var sql = (string)typeof(QueryWords).GetProperty(propertyName)!.GetValue(null)!;
        sql.ShouldNotContain("SourceMachineId");
    }

    [TestCase(nameof(QueryWords.GetWordsByFileIdOrderedByWordSql))]
    [TestCase(nameof(QueryWords.GetWordsByFileIdOrderedByOriginSql))]
    [TestCase(nameof(QueryWords.GetWordsByFilePathOrderedByWordSql))]
    public void ScopedWordLookupQueries_Should_Filter_By_SourceMachineId(string propertyName)
    {
        var sql = (string)typeof(QueryWords).GetProperty(propertyName)!.GetValue(null)!;
        sql.ShouldContain("SourceMachineId");
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

    [Test]
    public void DeleteWordSql_Should_Contain_Delete_From_Where()
    {
        var sql = QueryWords.DeleteWordSql;
        sql.ShouldContain("DELETE FROM");
        sql.ShouldContain("WHERE");
    }

    [Test]
    public void DeleteWordSql_Should_Delete_From_Words_Table_By_Id()
    {
        var sql = QueryWords.DeleteWordSql;
        sql.ShouldContain("\"Words\"");
        sql.ShouldContain("\"Id\" = @Id");
    }

    [Test]
    public void GetWordsByFileIdSql_Should_Select_WordId_Word_And_Origin()
    {
        var sql = QueryWords.GetWordsByFileIdSql;
        sql.ShouldContain("Select", Case.Insensitive);
        sql.ShouldContain("WordId", Case.Insensitive);
        sql.ShouldContain("Word", Case.Insensitive);
        sql.ShouldContain("Origin", Case.Insensitive);
    }

    [Test]
    public void GetWordsByFileIdSql_Should_Join_WordFiles_To_Words_And_Filter_By_FileId()
    {
        var sql = QueryWords.GetWordsByFileIdSql;
        sql.ShouldContain("\"WordFiles\"");
        sql.ShouldContain("JOIN", Case.Insensitive);
        sql.ShouldContain("\"Words\"");
        sql.ShouldContain("\"FileId\" = @FileId");
    }

    [Test]
    public void DeleteWordFileLinkSql_Should_Contain_Delete_From_Where()
    {
        var sql = QueryWords.DeleteWordFileLinkSql;
        sql.ShouldContain("DELETE FROM");
        sql.ShouldContain("WHERE");
    }

    [Test]
    public void DeleteWordFileLinkSql_Should_Delete_From_WordFiles_By_FileId_And_WordId()
    {
        var sql = QueryWords.DeleteWordFileLinkSql;
        sql.ShouldContain("\"WordFiles\"");
        sql.ShouldContain("\"FileId\" = @FileId");
        sql.ShouldContain("\"WordId\" = @WordId");
    }
}
