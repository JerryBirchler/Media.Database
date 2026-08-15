using AutoFixture.NUnit3;
using Media.Database.Models;
using NUnit.Framework;
using Shouldly;
using System;

namespace Media.Database.Tests.Models;

[TestFixture]
public class FilesTests
{
    [Test]
    public void This_Should_Have_DefaultValues()
    {
        var file = new Files();

        file.OriginalFilePath.ShouldBe(string.Empty);
        file.IsCurrent.ShouldBeTrue();
        file.Metadata.ShouldBeNull();
    }

    [Test, AutoData]
    public void This_Should_CreateFile_With_AutoFixtureValues(Files file)
    {
        file.Id.ShouldNotBe(Guid.Empty);
        file.OriginalFilePath.ShouldNotBeNull();
    }
}
