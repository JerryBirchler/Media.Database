using Cassandra;
using Media.Common.BackgroundJobs;
using Media.Common.Providers;
using Media.Common.Transactions;
using Media.Database.Mappers;
using Media.Database.Models;
using Media.Database.Repositories;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Serilog.Core;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Net;
using System.Reflection;
using Metadata = Media.Database.Models.Metadata;

namespace Media.Database.Tests.Repositories;

/// <summary>
/// Covers the private pure logic in FileRepository via reflection: GetUpdates and
/// IsScyllaConnectivityException. See FileRepositoryQueryTests for the public SQL-facing methods,
/// which are mockable via ISqlQueryExecutor.
/// </summary>
[TestFixture]
public class FileRepositoryLogicTests
{
    private static FileRepository CreateRepository(IMapChangeWordRequests mapper)
    {
        var scyllaProviderMock = new Mock<IScyllaSessionProvider>();
        scyllaProviderMock.Setup(p => p.MaxBatchSize).Returns(100);

        return new FileRepository(
            Mock.Of<ISqlQueryExecutor>(),
            scyllaProviderMock.Object,
            () => Mock.Of<IUnitOfWork>(),
            mapper,
            Mock.Of<IBackgroundTaskQueue>(),
            Mock.Of<ILogger<FileRepository>>(),
            new LoggingLevelSwitch());
    }

    private static List<ChangeWordRequest> InvokeGetUpdates(FileRepository repository, Files current, UpdateFileRequest request)
    {
        var method = typeof(FileRepository).GetMethod("GetUpdates", BindingFlags.NonPublic | BindingFlags.Instance)!;
        return (List<ChangeWordRequest>)method.Invoke(repository, [current, request])!;
    }

    private static bool InvokeIsScyllaConnectivityException(Exception ex)
    {
        var method = typeof(FileRepository).GetMethod("IsScyllaConnectivityException", BindingFlags.NonPublic | BindingFlags.Static)!;
        return (bool)method.Invoke(null, [ex])!;
    }

    [Test]
    public void GetUpdates_Should_ReturnEmptyList_When_CurrentAndNewMetadataAreBothNull()
    {
        var mapperMock = new Mock<IMapChangeWordRequests>();
        var repository = CreateRepository(mapperMock.Object);
        var current = new Files { Id = Guid.NewGuid(), Metadata = null };
        var request = new UpdateFileRequest { Metadata = null };

        var updates = InvokeGetUpdates(repository, current, request);

        updates.ShouldBeEmpty();
        mapperMock.Verify(m => m.ProcessList(It.IsAny<List<ChangeWordRequest>>(), It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>(), It.IsAny<Files>(), It.IsAny<WordOrigin>()), Times.Never);
        mapperMock.Verify(m => m.ProcessScalar(It.IsAny<List<ChangeWordRequest>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Files>(), It.IsAny<WordOrigin>()), Times.Never);
    }

    [Test]
    public void GetUpdates_Should_ProcessNamesAndKeywords_As_Lists()
    {
        var mapperMock = new Mock<IMapChangeWordRequests>();
        var repository = CreateRepository(mapperMock.Object);
        var current = new Files
        {
            Id = Guid.NewGuid(),
            Metadata = new Metadata { Names = ["alice"], KeyWords = ["kw1"] }
        };
        var request = new UpdateFileRequest
        {
            Metadata = new Metadata { Names = ["bob"], KeyWords = ["kw2"] }
        };

        InvokeGetUpdates(repository, current, request);

        mapperMock.Verify(m => m.ProcessList(
            It.IsAny<List<ChangeWordRequest>>(), current.Metadata.Names, request.Metadata.Names, current, WordOrigin.Name),
            Times.Once);
        mapperMock.Verify(m => m.ProcessList(
            It.IsAny<List<ChangeWordRequest>>(), current.Metadata.KeyWords, request.Metadata.KeyWords, current, WordOrigin.Keyword),
            Times.Once);
    }

    [Test]
    public void GetUpdates_Should_ProcessTitleDescriptionEventLocation_As_Scalars()
    {
        var mapperMock = new Mock<IMapChangeWordRequests>();
        var repository = CreateRepository(mapperMock.Object);
        var current = new Files
        {
            Id = Guid.NewGuid(),
            Metadata = new Metadata { Title = "old-title", Description = "old-desc", Event = "old-event", Location = "old-location" }
        };
        var request = new UpdateFileRequest
        {
            Metadata = new Metadata { Title = "new-title", Description = "new-desc", Event = "new-event", Location = "new-location" }
        };

        InvokeGetUpdates(repository, current, request);

        mapperMock.Verify(m => m.ProcessScalar(It.IsAny<List<ChangeWordRequest>>(), "old-title", "new-title", current, WordOrigin.FromTitle), Times.Once);
        mapperMock.Verify(m => m.ProcessScalar(It.IsAny<List<ChangeWordRequest>>(), "old-desc", "new-desc", current, WordOrigin.FromDescription), Times.Once);
        mapperMock.Verify(m => m.ProcessScalar(It.IsAny<List<ChangeWordRequest>>(), "old-event", "new-event", current, WordOrigin.FromEvent), Times.Once);
        mapperMock.Verify(m => m.ProcessScalar(It.IsAny<List<ChangeWordRequest>>(), "old-location", "new-location", current, WordOrigin.FromLocation), Times.Once);
    }

    [Test]
    public void GetUpdates_Should_ProcessAllFields_When_OnlyNewMetadataIsPresent()
    {
        var mapperMock = new Mock<IMapChangeWordRequests>();
        var repository = CreateRepository(mapperMock.Object);
        var current = new Files { Id = Guid.NewGuid(), Metadata = null };
        var request = new UpdateFileRequest { Metadata = new Metadata { Title = "new-title" } };

        InvokeGetUpdates(repository, current, request);

        mapperMock.Verify(m => m.ProcessScalar(It.IsAny<List<ChangeWordRequest>>(), null, "new-title", current, WordOrigin.FromTitle), Times.Once);
    }

    [Test]
    public void IsScyllaConnectivityException_Should_ReturnTrue_For_KnownConnectivityExceptions()
    {
        var noHost = new NoHostAvailableException(new Dictionary<IPEndPoint, Exception>());
        var unavailable = new UnavailableException(ConsistencyLevel.One, 1, 0);
        var timedOut = new OperationTimedOutException(new IPEndPoint(IPAddress.Loopback, 9042), 1000);

        InvokeIsScyllaConnectivityException(noHost).ShouldBeTrue();
        InvokeIsScyllaConnectivityException(unavailable).ShouldBeTrue();
        InvokeIsScyllaConnectivityException(timedOut).ShouldBeTrue();
    }

    [Test]
    public void IsScyllaConnectivityException_Should_ReturnFalse_For_UnrelatedException()
    {
        InvokeIsScyllaConnectivityException(new InvalidOperationException()).ShouldBeFalse();
    }
}
