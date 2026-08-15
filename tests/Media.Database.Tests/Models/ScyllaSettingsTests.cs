using AutoFixture.NUnit3;
using Media.Database.Models;
using NUnit.Framework;
using Shouldly;

namespace Media.Database.Tests.Models;

[TestFixture]
public class ScyllaSettingsTests
{
    [Test, AutoData]
    public void This_Should_Be_Constructible_WithRequiredProperties(ScyllaSettings settings)
    {
        settings.ContactPoints.ShouldNotBeNull();
        settings.ExternalContactPoints.ShouldNotBeNull();
        settings.Port.ShouldBeGreaterThan(0);
        settings.Keyspace.ShouldNotBeNullOrEmpty();
    }
}
