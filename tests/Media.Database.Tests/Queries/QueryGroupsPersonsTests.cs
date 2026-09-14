using Media.Database.Repositories.Queries;
using NUnit.Framework;
using Shouldly;

namespace Media.Database.Tests.Queries;

[TestFixture]
public class QueryGroupsPersonsTests
{
    [Test]
    public void UpsertSql_Should_Contain_With_Updated_Inserted_UnionAll()
    {
        var sql = QueryGroupsPersons.UpsertSql;
        sql.ShouldContain("WITH existing AS");
        sql.ShouldContain("updated AS");
        sql.ShouldContain("inserted AS");
        sql.ShouldContain("UNION ALL");
    }

    [Test]
    public void GetActiveSql_Should_Contain_Select_From_Where()
    {
        var sql = QueryGroupsPersons.GetActiveSql;
        sql.ShouldContain("SELECT");
        sql.ShouldContain("FROM");
        sql.ShouldContain("WHERE");
    }

    [Test]
    public void DeactivateSql_Should_Contain_Update_Where_Returning()
    {
        var sql = QueryGroupsPersons.DeactivateSql;
        sql.ShouldContain("UPDATE");
        sql.ShouldContain("WHERE");
        sql.ShouldContain("RETURNING");
    }

    [Test]
    public void CountActiveAdminsSql_Should_Contain_SelectCount_Where()
    {
        var sql = QueryGroupsPersons.CountActiveAdminsSql;
        sql.ShouldContain("SELECT COUNT(*)");
        sql.ShouldContain("WHERE");
    }

    [Test]
    public void GetGroupIdentifiersByPersonIdSql_Should_Contain_Select_Join_Where_OrderBy_Limit()
    {
        var sql = QueryGroupsPersons.GetGroupIdentifiersByPersonIdSql;
        sql.ShouldContain("SELECT");
        sql.ShouldContain("FROM");
        sql.ShouldContain("JOIN");
        sql.ShouldContain("WHERE");
        sql.ShouldContain("ORDER BY");
        sql.ShouldContain("@Limit");
        sql.ShouldContain("COALESCE");
    }

    [Test]
    public void GetGroupIdentifiersByPersonIdSql_Should_Scope_To_A_Single_PersonId()
    {
        var sql = QueryGroupsPersons.GetGroupIdentifiersByPersonIdSql;
        sql.ShouldContain("\"PersonId\" = @PersonId");
    }

    [Test]
    public void GetGroupIdentifiersByPersonIdSql_Should_OrderBy_Name()
    {
        var sql = QueryGroupsPersons.GetGroupIdentifiersByPersonIdSql;
        sql.ShouldContain("ORDER BY");
        sql.ShouldContain("\"Name\" ASC");
    }

    [Test]
    public void GetPersonIdentifiersByGroupIdSql_Should_Contain_Select_Join_Where_OrderBy_Limit()
    {
        var sql = QueryGroupsPersons.GetPersonIdentifiersByGroupIdSql;
        sql.ShouldContain("SELECT");
        sql.ShouldContain("FROM");
        sql.ShouldContain("JOIN");
        sql.ShouldContain("WHERE");
        sql.ShouldContain("ORDER BY");
        sql.ShouldContain("@Limit");
        sql.ShouldContain("COALESCE");
    }

    [Test]
    public void GetPersonIdentifiersByGroupIdSql_Should_Scope_To_A_Single_GroupId()
    {
        var sql = QueryGroupsPersons.GetPersonIdentifiersByGroupIdSql;
        sql.ShouldContain("\"GroupId\" = @GroupId");
    }

    [Test]
    public void GetPersonIdentifiersByGroupIdSql_Should_OrderBy_LastName_Then_FirstName_Then_PersonUuid()
    {
        var sql = QueryGroupsPersons.GetPersonIdentifiersByGroupIdSql;
        sql.ShouldContain("\"LastName\" ASC");
        sql.ShouldContain("\"FirstName\" ASC");
        sql.ShouldContain("\"PersonUuid\" ASC");
    }

    // ToGroupPerson/ToGroupIdentifier(s)/ToPersonIdentifier(s) (NpgsqlDataReader) are not
    // unit-testable: NpgsqlDataReader is sealed and cannot be mocked (see QueryFilesTests).
}
