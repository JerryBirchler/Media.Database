using AutoFixture.NUnit3;
using Media.Database.Configuration;
using NUnit.Framework;
using Shouldly;

namespace Media.Database.Tests.Configuration;

[TestFixture]
public class PostgresOptionsTests
{
    [Test]
    public void PostgresOptions_Should_HaveExpectedDefaults()
    {
        var options = new PostgresOptions();

        options.PostgresConnection.ShouldBe(string.Empty);
    }

    [Test]
    public void PostgresOptions_Should_ExposeSectionName()
    {
        PostgresOptions.SectionName.ShouldBe("ConnectionStrings");
    }

    [Test, AutoData]
    public void PostgresOptions_Should_RoundTrip_PostgresConnection(string connectionString)
    {
        var options = new PostgresOptions { PostgresConnection = connectionString };

        options.PostgresConnection.ShouldBe(connectionString);
    }
}
