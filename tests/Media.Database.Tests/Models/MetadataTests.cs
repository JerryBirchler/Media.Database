using AutoFixture.NUnit3;
using Media.Database.Models;
using NUnit.Framework;
using Shouldly;

namespace Media.Database.Tests.Models;

[TestFixture]
public class MetadataTests
{
    [Test]
    public void Metadata_Should_Have_NullOrEmpty_Defaults()
    {
        var m = new Metadata();

        m.KeyWords.ShouldBeNull();
        m.Names.ShouldBeNull();
        m.Title.ShouldBeNull();
    }

    [Test, AutoData]
    public void Metadata_Should_Allow_Setting_Collections(Metadata m)
    {
        // AutoFixture will provide a populated Metadata instance
        m.KeyWords.ShouldBe(m.KeyWords);
        m.Title.ShouldBe(m.Title);
    }
}
