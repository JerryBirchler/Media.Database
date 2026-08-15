using Media.Database.Helpers;
using Media.Database.Models;
using NUnit.Framework;
using Shouldly;
using System.Net;

namespace Media.Database.Tests.Helpers;

[TestFixture]
public class DockerPortTranslatorTests
{
    [Test]
    public void This_Should_Translate_To_TranslatedEndpoint_When_Address_Matches_ExternalContactPoint()
    {
        var settings = new ScyllaSettings
        {
            ContactPoints = new System.Collections.Generic.List<System.Uri> { new System.Uri("http://127.0.0.1") },
            ExternalContactPoints = new System.Collections.Generic.List<System.Uri> { new System.Uri("http://10.1.1.1") },
            Port = 9000,
            Keyspace = "ks",
            MaxBatchsize = 100
        };

        var translator = new DockerPortTranslator(settings);

        var incoming = new IPEndPoint(System.Net.IPAddress.Parse("10.1.1.1"), 9042);

        var translated = translator.Translate(incoming);

        translated.Address.ToString().ShouldBe("127.0.0.1");
        translated.Port.ShouldBe(9000 + 0);
    }

    [Test]
    public void This_Should_Return_Original_Port_With_ContactPointIp_When_NoMatch()
    {
        var settings = new ScyllaSettings
        {
            ContactPoints = new System.Collections.Generic.List<System.Uri> { new System.Uri("http://127.0.0.1") },
            ExternalContactPoints = new System.Collections.Generic.List<System.Uri> { new System.Uri("http://10.1.1.1") },
            Port = 9000,
            Keyspace = "ks",
            MaxBatchsize = 100
        };

        var translator = new DockerPortTranslator(settings);

        var incoming = new IPEndPoint(System.Net.IPAddress.Parse("8.8.8.8"), 1234);

        var translated = translator.Translate(incoming);

        translated.Address.ToString().ShouldBe("127.0.0.1");
        translated.Port.ShouldBe(1234);
    }
}
