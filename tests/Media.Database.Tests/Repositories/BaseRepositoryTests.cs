using AutoFixture;
using Cassandra;
using Media.Common.Providers;
using Media.Database.Repositories;
using Media.Database.Tests.TestHelpers;
using Moq;
using NUnit.Framework;
using Shouldly;
using System;

namespace Media.Database.Tests.Repositories;

[TestFixture]
public class BaseRepositoryTests
{
    private sealed class TestableRepository : BaseRepository
    {
        public TestableRepository(IScyllaSessionProvider scyllaProvider)
            : base(scyllaProvider)
        {
        }

        public TestableRepository()
            : base()
        {
        }
    }

    [Test]
    public void Constructor_Should_ThrowArgumentNullException_When_ScyllaProviderIsNull()
    {
        Should.Throw<ArgumentNullException>(() => new TestableRepository(null!))
            .ParamName.ShouldBe("scyllaProvider");
    }

    [Test]
    public void GetNoSqlConnection_Should_ReturnSession_When_ScyllaProviderConfigured()
    {
        var fixture = AutoMoqFixture.Create();
        var session = Mock.Of<ISession>();
        var scyllaProviderMock = fixture.Freeze<Mock<IScyllaSessionProvider>>();
        scyllaProviderMock.Setup(p => p.GetSession()).Returns(session);

        var repository = new TestableRepository(scyllaProviderMock.Object);

        repository.GetNoSqlConnection().ShouldBe(session);
    }

    [Test]
    public void GetNoSqlConnection_Should_ThrowInvalidOperationException_When_ScyllaProviderNotInitialized()
    {
        var repository = new TestableRepository();

        Should.Throw<InvalidOperationException>(() => repository.GetNoSqlConnection())
            .Message.ShouldBe("Scylla provider not initialized");
    }
}
