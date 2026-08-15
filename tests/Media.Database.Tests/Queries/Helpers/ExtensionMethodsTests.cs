using AutoFixture.NUnit3;
using Media.Database.Repositories.Queries.Helpers;
using NUnit.Framework;
using Shouldly;
using System;
using System.Collections.Generic;

namespace Media.Database.Tests.Queries.Helpers;

[TestFixture]
public class ExtensionMethodsTests
{
    [Test, AutoData]
    public void This_Should_AdjustPrecision_To_Millisecond_Precision(DateTimeOffset dt)
    {
        var adjusted = ExtensionMethods.AdjustPrecision(dt);

        (adjusted.Ticks % 10000).ShouldBe(0);
    }

    [Test]
    public void This_Should_Return_EmptyString_When_Model_Is_Null()
    {
        object model = null;
        var result = ExtensionMethods.ToJsonString(model);

        result.ShouldBe(string.Empty);
    }

    [Test, AutoData]
    public void This_Should_Return_DBNull_For_Null(string s)
    {
        s = null;
        var result = ExtensionMethods.ToNullableValueForSql(s);
        result.ShouldBe(DBNull.Value);
    }

    [Test, AutoData]
    public void This_Should_Add_Key_In_Uppercase(string key, int value)
    {
        var dict = new SortedDictionary<string, object>();
        dict.AddWithValue(key, value);
        dict.ContainsKey(key.ToUpperInvariant()).ShouldBeTrue();
        dict[key.ToUpperInvariant()].ShouldBe(value);
    }
}
