using AutoFixture;
using Cassandra;
using Media.Common.Helpers.Fluent;
using Media.Common.Providers;
using Media.Database.Repositories;
using Media.Database.Tests.TestHelpers;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

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

        public static bool InvokeIsScyllaConnectivityException(Exception ex) => IsScyllaConnectivityException(ex);

        public Task InvokeTryHealScyllaSessionAsync<T>(FluentLogger<T> logger, string methodName) =>
            TryHealScyllaSessionAsync(logger, methodName);
    }

    [Test]
    public void Constructor_Should_ThrowArgumentNullException_When_ScyllaProviderIsNull()
    {
        Should.Throw<ArgumentNullException>(() => new TestableRepository(null!))
            .ParamName.ShouldBe("scyllaProvider");
    }

    [Test]
    public void GetCqlConnection_Should_ReturnSession_When_ScyllaProviderConfigured()
    {
        var fixture = AutoMoqFixture.Create();
        var session = Mock.Of<ISession>();
        var scyllaProviderMock = fixture.Freeze<Mock<IScyllaSessionProvider>>();
        scyllaProviderMock.Setup(p => p.GetSession()).Returns(session);

        var repository = new TestableRepository(scyllaProviderMock.Object);

        repository.GetCqlConnection().ShouldBe(session);
    }

    [Test]
    public void GetCqlConnection_Should_ThrowInvalidOperationException_When_ScyllaProviderNotInitialized()
    {
        var repository = new TestableRepository();

        Should.Throw<InvalidOperationException>(() => repository.GetCqlConnection())
            .Message.ShouldBe("Scylla provider not initialized");
    }

    [Test]
    public void IsScyllaConnectivityException_Should_ReturnTrue_For_KnownConnectivityExceptions()
    {
        var noHost = new NoHostAvailableException(new Dictionary<IPEndPoint, Exception>());
        var unavailable = new UnavailableException(ConsistencyLevel.One, 1, 0);
        var timedOut = new OperationTimedOutException(new IPEndPoint(IPAddress.Loopback, 9042), 1000);

        TestableRepository.InvokeIsScyllaConnectivityException(noHost).ShouldBeTrue();
        TestableRepository.InvokeIsScyllaConnectivityException(unavailable).ShouldBeTrue();
        TestableRepository.InvokeIsScyllaConnectivityException(timedOut).ShouldBeTrue();
    }

    [Test]
    public void IsScyllaConnectivityException_Should_ReturnFalse_For_UnrelatedException()
    {
        TestableRepository.InvokeIsScyllaConnectivityException(new InvalidOperationException()).ShouldBeFalse();
    }

    [Test]
    public async Task TryHealScyllaSessionAsync_Should_CallHealSessionAsync_With_CurrentSessionIdAndMethodName()
    {
        var fixture = AutoMoqFixture.Create();
        var scyllaProviderMock = fixture.Freeze<Mock<IScyllaSessionProvider>>();
        var sessionId = Guid.NewGuid();
        scyllaProviderMock.Setup(p => p.GetCurrentSessionId()).Returns(sessionId);
        scyllaProviderMock.Setup(p => p.HealSessionAsync(sessionId, "SomeMethod")).Returns(Task.CompletedTask);
        var repository = new TestableRepository(scyllaProviderMock.Object);
        var logger = Mock.Of<ILogger<BaseRepositoryTests>>().Initializer();

        await repository.InvokeTryHealScyllaSessionAsync(logger, "SomeMethod");

        scyllaProviderMock.Verify(p => p.HealSessionAsync(sessionId, "SomeMethod"), Times.Once);
    }

    [Test]
    public void TryHealScyllaSessionAsync_Should_SwallowHealFailure_Without_Throwing()
    {
        var fixture = AutoMoqFixture.Create();
        var scyllaProviderMock = fixture.Freeze<Mock<IScyllaSessionProvider>>();
        scyllaProviderMock.Setup(p => p.GetCurrentSessionId()).Returns(Guid.NewGuid());
        scyllaProviderMock.Setup(p => p.HealSessionAsync(It.IsAny<Guid>(), It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException("heal failed"));
        var repository = new TestableRepository(scyllaProviderMock.Object);
        var logger = Mock.Of<ILogger<BaseRepositoryTests>>().Initializer();

        Should.NotThrowAsync(() => repository.InvokeTryHealScyllaSessionAsync(logger, "SomeMethod"));
    }
}
