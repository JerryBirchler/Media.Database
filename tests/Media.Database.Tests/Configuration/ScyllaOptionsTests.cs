using AutoFixture.NUnit3;
using Media.Database.Configuration;
using NUnit.Framework;
using Shouldly;
using System.Collections.Generic;

namespace Media.Database.Tests.Configuration;

[TestFixture]
public class ScyllaOptionsTests
{
    [Test]
    public void ScyllaOptions_Should_HaveExpectedDefaults()
    {
        var options = new ScyllaOptions();

        options.ContactPoints.ShouldBeEmpty();
        options.ExternalContactPoints.ShouldBeEmpty();
        options.Port.ShouldBe(0);
        options.Keyspace.ShouldBe(string.Empty);
        options.MaxBatchsize.ShouldBe(100);
    }

    [Test]
    public void ScyllaOptions_Should_ExposeSectionName()
    {
        ScyllaOptions.SectionName.ShouldBe("ScyllaDB");
    }

    [Test, AutoData]
    public void ScyllaOptions_Should_RoundTrip_Properties(
        List<string> contactPoints,
        List<string> externalContactPoints,
        int port,
        string keyspace,
        int maxBatchsize)
    {
        var options = new ScyllaOptions
        {
            ContactPoints = contactPoints,
            ExternalContactPoints = externalContactPoints,
            Port = port,
            Keyspace = keyspace,
            MaxBatchsize = maxBatchsize
        };

        options.ContactPoints.ShouldBe(contactPoints);
        options.ExternalContactPoints.ShouldBe(externalContactPoints);
        options.Port.ShouldBe(port);
        options.Keyspace.ShouldBe(keyspace);
        options.MaxBatchsize.ShouldBe(maxBatchsize);
    }
}
