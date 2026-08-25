using AutoFixture.NUnit3;
using Cassandra;
using Media.Database.Repositories.Queries.Helpers;
using Moq;
using NUnit.Framework;
using Shouldly;
using System;
using System.Collections.Generic;

namespace Media.Database.Tests.Queries.Helpers;

[TestFixture]
public class ExtensionMethodsTests
{
    [Test, AutoData]
    public void AdjustPrecision_Should_Adjust_To_Millisecond_Precision(DateTimeOffset dt)
    {
        var adjusted = ExtensionMethods.AdjustPrecision(dt);

        (adjusted.Ticks % 10000).ShouldBe(0);
    }

    [Test, AutoData]
    public void AdjustPrecision_Nullable_Should_Adjust_To_Millisecond_Precision(DateTimeOffset dt)
    {
        DateTimeOffset? nullable = dt;

        var adjusted = ExtensionMethods.AdjustPrecision(nullable);

        adjusted.HasValue.ShouldBeTrue();
        (adjusted!.Value.Ticks % 10000).ShouldBe(0);
    }

    [Test]
    public void AdjustPrecision_Nullable_Should_Return_Null_When_Timestamp_Is_Null()
    {
        DateTimeOffset? timestamp = null;

        var adjusted = ExtensionMethods.AdjustPrecision(timestamp);

        adjusted.ShouldBeNull();
    }

    [Test]
    public void ToJsonString_Should_Return_EmptyString_When_Model_Is_Null()
    {
        object model = null;
        var result = ExtensionMethods.ToJsonString(model);

        result.ShouldBe(string.Empty);
    }

    [Test]
    public void ToJsonString_Should_Return_Model_Unchanged_When_Model_Is_DBNull()
    {
        var result = ExtensionMethods.ToJsonString(DBNull.Value);

        result.ShouldBe(DBNull.Value);
    }

    [Test]
    public void ToJsonString_Should_Return_SerializedJson_When_Model_Is_NotNull()
    {
        var model = new { Name = "test" };

        var result = ExtensionMethods.ToJsonString(model);

        result.ShouldBe("{\"Name\":\"test\"}");
    }

    [Test, AutoData]
    public void ToDbNull_Should_Return_DBNull_For_Null(string s)
    {
        s = null;
        var result = ExtensionMethods.ToNullableValueForSql(s);
        result.ShouldBe(DBNull.Value);
    }

    [Test, AutoData]
    public void ToNullableValueForSql_Should_Return_Value_When_NotNull(string value)
    {
        var result = ExtensionMethods.ToNullableValueForSql(value);

        result.ShouldBe(value);
    }

    [Test, AutoData]
    public void AddWithKeyUpper_Should_Add_Key_In_Uppercase(string key, int value)
    {
        var dict = new SortedDictionary<string, object>();
        dict.AddWithValue(key, value);
        dict.ContainsKey(key.ToUpperInvariant()).ShouldBeTrue();
        dict[key.ToUpperInvariant()].ShouldBe(value);
    }

    [Test, AutoData]
    public void GetNoSqlCommand_Should_Return_NoSqlCommand_Wrapping_Session(string query)
    {
        var session = Mock.Of<ISession>();

        var command = session.GetNoSqlCommand(query);

        command.ShouldNotBeNull();
        command.Parameters.ShouldBeEmpty();
    }

    // GetValueOrDefault<T>(Row, string) is not unit-testable: it calls Row.IsNull(string),
    // which is a non-overridable member on the Cassandra driver's Row type, so Moq cannot
    // stub it. Exercising this method requires a real Cassandra row (integration-test territory).
}
