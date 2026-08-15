using Media.Database.Models;
using NUnit.Framework;
using Shouldly;

namespace Media.Database.Tests.Models;

[TestFixture]
public class WordOriginTests
{
    [Test]
    public void This_Should_Contain_Expected_EnumValues()
    {
        var names = System.Enum.GetNames(typeof(WordOrigin));

        names.ShouldContain("Name");
        names.ShouldContain("FromTitle");
        names.Length.ShouldBeGreaterThanOrEqualTo(1);
    }
}
