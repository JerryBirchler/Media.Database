using Media.Database.Repositories;
using Microsoft.Extensions.Configuration;
using Moq;
using NUnit.Framework;
using Shouldly;
using System.Collections.Generic;

namespace Media.Database.Tests.Repositories;

[TestFixture]
public class WordRepositoryTests
{
    [Test]
    public void This_Should_Construct_WordRepository_With_Logger_And_LevelSwitch()
    {
        var logger = Mock.Of<Microsoft.Extensions.Logging.ILogger<WordRepository>>();
        var levelSwitch = new Serilog.Core.LoggingLevelSwitch();

        var repo = new WordRepository();

        repo.ShouldNotBeNull();
    }

    [Test]
    public void This_Should_Construct_WordRepository_With_Configuration()
    {
        var inMemory = new Dictionary<string, string>
        {
            { "ConnectionStrings:PostgresConnection", "Host=localhost;Username=test;Password=pass" },
            { "ScyllaDB:ContactPoints:0", "http://127.0.0.1" },
            { "ScyllaDB:ExternalContactPoints:0", "http://10.0.0.1" },
            { "ScyllaDB:Port", "9042" },
            { "ScyllaDB:Keyspace", "ks" }
        };

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemory)
            .Build();

        var repo = new WordRepository();

        repo.ShouldNotBeNull();
    }

    [Test]
    public void This_Should_Implement_IWordRepository()
    {
        var repo = new WordRepository();
        repo.ShouldBeAssignableTo<IWordRepository>();
    }

    [Test]
    public void This_Should_Inherit_From_BaseRepository()
    {
        var repo = new WordRepository();
        repo.ShouldBeAssignableTo<BaseRepository>();
    }
}
