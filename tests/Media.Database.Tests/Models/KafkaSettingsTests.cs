using AutoFixture.NUnit3;
using Media.Database.Models;
using NUnit.Framework;
using Shouldly;

namespace Media.Database.Tests.Models;

[TestFixture]
public class KafkaSettingsTests
{
    [Test, AutoData]
    public void KafkaSettings_Should_Be_Constructible_With_RequiredProperties(KafkaSettings settings)
    {
        settings.BaseUrl.ShouldNotBeNull();
        settings.Port.ShouldBeGreaterThan(0);
        settings.ClusterId.ShouldNotBeNullOrEmpty();
    }
}
