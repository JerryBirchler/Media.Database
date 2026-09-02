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
using System.Reflection;
using Metadata = Media.Database.Models.Metadata;

namespace Media.Database.Tests.Repositories;

/// <summary>
/// Covers the private pure logic in FileRepository via reflection: GetUpdates. See
/// FileRepositoryQueryTests for the public SQL-facing methods, which are mockable via
/// ISqlQueryExecutor, and BaseRepositoryTests for IsScyllaConnectivityException/
/// TryHealScyllaSessionAsync, which live on the shared base class now.
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
            Mock.Of<ICqlQueryExecutor>(),
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
    public void GetUpdates_Should_ProcessAllFields_When_OnlyCurrentMetadataIsPresent()
    {
        var mapperMock = new Mock<IMapChangeWordRequests>();
        var repository = CreateRepository(mapperMock.Object);
        var current = new Files
        {
            Id = Guid.NewGuid(),
            Metadata = new Metadata { Names = ["alice"], KeyWords = ["kw1"], Title = "old-title", Description = "old-desc", Event = "old-event", Location = "old-location" }
        };
        var request = new UpdateFileRequest { Metadata = null };

        InvokeGetUpdates(repository, current, request);

        mapperMock.Verify(m => m.ProcessList(It.IsAny<List<ChangeWordRequest>>(), current.Metadata.Names, null, current, WordOrigin.Name), Times.Once);
        mapperMock.Verify(m => m.ProcessList(It.IsAny<List<ChangeWordRequest>>(), current.Metadata.KeyWords, null, current, WordOrigin.Keyword), Times.Once);
        mapperMock.Verify(m => m.ProcessScalar(It.IsAny<List<ChangeWordRequest>>(), "old-title", null, current, WordOrigin.FromTitle), Times.Once);
        mapperMock.Verify(m => m.ProcessScalar(It.IsAny<List<ChangeWordRequest>>(), "old-desc", null, current, WordOrigin.FromDescription), Times.Once);
        mapperMock.Verify(m => m.ProcessScalar(It.IsAny<List<ChangeWordRequest>>(), "old-event", null, current, WordOrigin.FromEvent), Times.Once);
        mapperMock.Verify(m => m.ProcessScalar(It.IsAny<List<ChangeWordRequest>>(), "old-location", null, current, WordOrigin.FromLocation), Times.Once);
    }
}
