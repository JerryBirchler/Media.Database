using Media.Database.Repositories.Queries;
using NUnit.Framework;
using Shouldly;

namespace Media.Database.Tests.Queries;

[TestFixture]
public class QueryPersonsTests
{
    [Test]
    public void GetPersonIdentifiersByCreatorSql_Should_IncludeCallerThemselves_Not_JustCreatedPersons()
    {
        // A caller must see their own row in GET /api/persons/{next}/pages, not just persons they
        // created -- person zero (CreatedByPersonId IS NULL) would otherwise never see themselves.
        var sql = QueryPersons.GetPersonIdentifiersByCreatorSql;
        sql.ShouldContain("\"CreatedByPersonId\" = @CreatedByPersonId");
        sql.ShouldContain("\"PersonId\" = @CreatedByPersonId");
        sql.ShouldContain(" OR ");
    }
}
