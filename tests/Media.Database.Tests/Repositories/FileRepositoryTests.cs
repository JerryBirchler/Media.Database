using Media.Common.BackgroundJobs;
using Media.Common.Providers;
using Media.Common.Transactions;
using Media.Database.Mappers;
using Media.Database.Repositories;
using Microsoft.Extensions.Configuration;
using Moq;
using NUnit.Framework;
using Shouldly;
using System.Collections.Generic;

namespace Media.Database.Tests.Repositories;

[TestFixture]
public class FileRepositoryTests
{
    [Test]
    public void FileRepository_Constructor_Should_Accept_Configuration()
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

        var repo = new FileRepository(
            Mock.Of<IPostgresConnectionProvider>(),
            Mock.Of<IScyllaSessionProvider>(),
            () => Mock.Of<IUnitOfWork>(),
            Mock.Of<IMapChangeWordRequests>(),
            Mock.Of<IBackgroundTaskQueue>(),
            Mock.Of<Microsoft.Extensions.Logging.ILogger<FileRepository>>(),
            new Serilog.Core.LoggingLevelSwitch());

        repo.ShouldNotBeNull();
    }
}
