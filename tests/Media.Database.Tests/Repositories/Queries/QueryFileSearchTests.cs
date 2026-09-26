#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Media.Database.Models;
using Media.Database.Repositories.Queries;
using NUnit.Framework;
using Shouldly;

namespace Media.Database.Tests.Repositories.Queries;

/// <summary>
/// The search statement is built rather than written, so what it contains is only knowable by
/// building it. These assert the properties that matter -- sargability, the optional-filter
/// pattern, and the relational division -- rather than the exact text, which would break on every
/// whitespace change without catching a single real fault.
/// </summary>
[TestFixture]
public class QueryFileSearchTests
{
    private static IReadOnlyList<SearchListLine> Line(string word, WordOrigin? origin = null, WordType? type = null) =>
        [new SearchListLine { LookingFor = word, MetadataType = origin, WordType = type }];

    private static (string Sql, IReadOnlyDictionary<string, object> Parameters) Build(
        IReadOnlyList<IReadOnlyList<SearchListLine>> lists,
        bool? isCurrent = true,
        IReadOnlyList<int>? devices = null,
        int limit = 100) =>
        QueryFileSearch.Build(lists, isCurrent, devices ?? [], limit);

    [Test]
    public void Build_Should_SeekTheWordIndex_RatherThanScanTheView()
    {
        var (sql, parameters) = Build([Line("beach")]);

        // Word = ANY(...) is what keeps this an index seek. Without it the group-by reads the
        // whole view, which is the difference between a search and a table scan.
        sql.ShouldContain("= ANY(@words::text[])");
        ((string[])parameters["@words"]).ShouldBe(["beach"]);
    }

    [Test]
    public void Build_Should_NeverWrapTheWordColumnOrLeadWithAWildcard()
    {
        var (sql, _) = Build([Line("beach")]);

        // The one rule that keeps exact matching fast. A "helpful" LIKE added later is precisely
        // what would ruin it, so it is asserted rather than trusted.
        sql.ShouldNotContain("LIKE");
        sql.ShouldNotContain("lower(");
        sql.ShouldNotContain("upper(");
    }

    [Test]
    public void Build_Should_OrTheLinesWithinAList()
    {
        var (sql, parameters) = Build([[
            new SearchListLine { LookingFor = "beach" },
            new SearchListLine { LookingFor = "shore" },
        ]]);

        sql.ShouldContain(" OR ");
        parameters["@w0_0"].ShouldBe("beach");
        parameters["@w0_1"].ShouldBe("shore");
    }

    [Test]
    public void Build_Should_AndTheListsWithOneBoolOrEach()
    {
        var (sql, _) = Build([Line("Belle"), Line("beach"), Line("2019")]);

        // Relational division: a file qualifies when, among its word rows, at least one satisfies
        // every list. One bool_or per list, ANDed.
        sql.Split("bool_or").Length.ShouldBe(4);
        sql.ShouldContain("HAVING");
        sql.ShouldContain("GROUP BY");
    }

    [Test]
    public void Build_Should_TreatAnEmptyListAsVacuouslyTrue()
    {
        var (sql, _) = Build([Line("beach"), []]);

        // Dropped, not matched against. An emptied filter must not silently return zero rows --
        // it is the shape of bug that looks like missing data.
        sql.Split("bool_or").Length.ShouldBe(2);
    }

    [Test]
    public void Build_Should_OmitTheHaving_When_NothingWasAskedFor()
    {
        var (sql, parameters) = Build([]);

        sql.ShouldNotContain("HAVING");
        parameters.ContainsKey("@words").ShouldBeFalse();
    }

    [Test]
    public void Build_Should_PassAbsentRestrictionsAsNull()
    {
        var (_, parameters) = Build([Line("beach")]);

        // Absent means any. Expressed as a null parameter rather than by omitting the term, so
        // every line produces the same statement shape and Postgres can reuse the plan.
        parameters["@o0_0"].ShouldBe(DBNull.Value);
        parameters["@t0_0"].ShouldBe(DBNull.Value);
    }

    [Test]
    public void Build_Should_PassRestrictionsAsTheirNumericValues()
    {
        var (_, parameters) = Build([Line("Belle", WordOrigin.Name, WordType.ProperNoun)]);

        parameters["@o0_0"].ShouldBe((int)WordOrigin.Name);
        parameters["@t0_0"].ShouldBe((int)WordType.ProperNoun);
    }

    [Test]
    public void Build_Should_CastEveryOptionalParameter()
    {
        var (sql, _) = Build([Line("beach")], isCurrent: null);

        // A null passed as DBNull gives Npgsql no type to infer, and Postgres rejects the
        // statement outright rather than treating it as "any". The casts are load-bearing.
        sql.ShouldContain("@isCurrent::boolean");
        sql.ShouldContain("@sourceMachineIds::int[]");
        sql.ShouldContain("@o0_0::int");
        sql.ShouldContain("@t0_0::int");
    }

    [TestCase(true)]
    [TestCase(false)]
    public void Build_Should_CarryTheCurrentFilter_When_OneWasGiven(bool isCurrent)
    {
        var (_, parameters) = Build([Line("beach")], isCurrent);

        parameters["@isCurrent"].ShouldBe(isCurrent);
    }

    [Test]
    public void Build_Should_PassNullForTheCurrentFilter_When_BothAreWanted()
    {
        var (_, parameters) = Build([Line("beach")], isCurrent: null);

        parameters["@isCurrent"].ShouldBe(DBNull.Value);
    }

    [Test]
    public void Build_Should_PassNullForDevices_When_AnyDeviceWillDo()
    {
        var (_, parameters) = Build([Line("beach")], devices: []);

        parameters["@sourceMachineIds"].ShouldBe(DBNull.Value);
    }

    [Test]
    public void Build_Should_PassTheDevices_When_SomeWereNamed()
    {
        var (_, parameters) = Build([Line("beach")], devices: [17, 42]);

        ((int[])parameters["@sourceMachineIds"]).ShouldBe([17, 42]);
    }

    [Test]
    public void Build_Should_DedupeTheWordsCaseInsensitively()
    {
        var (_, parameters) = Build([[
            new SearchListLine { LookingFor = "Beach" },
            new SearchListLine { LookingFor = "beach" },
        ]]);

        // The column collates case-insensitively, so two spellings are one lookup.
        ((string[])parameters["@words"]).Length.ShouldBe(1);
    }

    [Test]
    public void Build_Should_OrderByPathAndCapTheResult()
    {
        var (sql, parameters) = Build([Line("beach")], limit: 25);

        // Ordered the way files already page, so a cursor can be layered on later without
        // changing what a page contains.
        sql.ShouldContain("ORDER BY");
        sql.ShouldContain("LIMIT @limit");
        parameters["@limit"].ShouldBe(25);
    }

    [Test]
    public void Build_Should_BindEveryValue_And_InterpolateNoneOfThem()
    {
        var (sql, parameters) = Build([Line("'; drop table \"Words\"; --")]);

        // Only the generated parameter name reaches the statement. Nothing a user typed is ever
        // part of the text.
        sql.ShouldNotContain("drop table");
        parameters["@w0_0"].ShouldBe("'; drop table \"Words\"; --");
    }
}
