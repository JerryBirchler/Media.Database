using AutoFixture.NUnit3;
using Media.Database.Models;
using NUnit.Framework;
using Shouldly;

namespace Media.Database.Tests.Models;

[TestFixture]
public class SourceMachineTests
{
    [Test]
    public void SourceMachine_Should_Have_DefaultValues()
    {
        var s = new SourceMachine();

        s.Name.ShouldBe(string.Empty);
        s.MetaData.ShouldBeNull();
    }

    [Test, AutoData]
    public void SourceMachine_Should_Be_Populatable_By_AutoFixture(SourceMachine s)
    {
        s.ShouldNotBeNull();
        s.Id.ShouldBeGreaterThanOrEqualTo(0);
        s.Name.ShouldNotBeNullOrEmpty();
    }

    [Test, AutoData]
    public void SourceMachine_Should_Be_Created_By_AutoFixture(SourceMachine s)
    {
        s.ShouldNotBeNull();
        s.Name.ShouldNotBeNullOrEmpty();
        s.InsertedOn.ShouldNotBe(default);
    }
}
