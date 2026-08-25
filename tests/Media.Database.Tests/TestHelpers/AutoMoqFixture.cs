using AutoFixture;
using AutoFixture.AutoMoq;

namespace Media.Database.Tests.TestHelpers;

internal static class AutoMoqFixture
{
    public static IFixture Create() => new Fixture().Customize(new AutoMoqCustomization());
}
