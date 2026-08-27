#nullable enable
using AutoFixture;
using Cassandra;
using Media.Common.BackgroundJobs;
using Media.Common.Providers;
using Media.Common.Transactions;
using Media.Database.Mappers;
using Media.Database.Models;
using Media.Database.Repositories;
using Media.Database.Repositories.Queries;
using Media.Database.Tests.TestHelpers;
using Microsoft.Extensions.Logging;
using Moq;
using Npgsql;
using NUnit.Framework;
using Serilog.Core;
using Shouldly;
using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Media.Database.Tests.Repositories;

/// <summary>
/// Covers FileRepository's fire-and-forget background NoSQL sync (queued via IBackgroundTaskQueue
/// after a successful SQL write). Per the dotnet-test-standards "fire-and-forget Task.Run" gotcha,
/// we capture the queued callback and invoke it directly rather than racing a background thread.
/// ISession is fully mockable, so these branches - including the Scylla-connectivity self-heal path -
/// are testable without touching a real cluster.
/// </summary>
[TestFixture]
public class FileRepositoryBackgroundCqlTests
{
    private Mock<ISqlQueryExecutor> _sqlExecutorMock = null!;
    private Mock<IScyllaSessionProvider> _scyllaProviderMock = null!;
    private Mock<ISession> _sessionMock = null!;
    private Mock<IUnitOfWork> _unitOfWorkMock = null!;
    private Func<CancellationToken, ValueTask>? _capturedCallback;
    private IFixture _fixture = null!;

    [SetUp]
    public void Setup()
    {
        _fixture = AutoMoqFixture.Create();
        _sqlExecutorMock = new Mock<ISqlQueryExecutor>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _sessionMock = new Mock<ISession>();
        _capturedCallback = null;

        _scyllaProviderMock = new Mock<IScyllaSessionProvider>();
        _scyllaProviderMock.Setup(p => p.MaxBatchSize).Returns(100);
        _scyllaProviderMock.Setup(p => p.GetSession()).Returns(_sessionMock.Object);

        var preparedStatementMock = new Mock<PreparedStatement>();
        preparedStatementMock.Setup(ps => ps.Bind(It.IsAny<object[]>())).Returns(new Mock<BoundStatement>().Object);
        _sessionMock.Setup(s => s.Prepare(It.IsAny<string>())).Returns(preparedStatementMock.Object);
        _sessionMock.Setup(s => s.ExecuteAsync(It.IsAny<Statement>())).ReturnsAsync(new Mock<RowSet>().Object);
    }

    private FileRepository CreateRepository(IBackgroundTaskQueue backgroundTaskQueue) => new(
        _sqlExecutorMock.Object,
        _scyllaProviderMock.Object,
        () => _unitOfWorkMock.Object,
        Mock.Of<IMapChangeWordRequests>(),
        backgroundTaskQueue,
        Mock.Of<ILogger<FileRepository>>(),
        new LoggingLevelSwitch());

    private Mock<IBackgroundTaskQueue> CaptureBackgroundTaskQueue()
    {
        var mock = new Mock<IBackgroundTaskQueue>();
        mock.Setup(q => q.QueueBackgroundWorkItemAsync(It.IsAny<Func<CancellationToken, ValueTask>>()))
            .Callback<Func<CancellationToken, ValueTask>>(cb => _capturedCallback = cb)
            .Returns(ValueTask.CompletedTask);
        return mock;
    }

    [Test]
    public async Task Upsert_BackgroundUpdate_Should_InactivatePreviousIds_And_UpsertCql_When_PreviousIdsExist()
    {
        var insertedFile = _fixture.Create<Files>();
        var previousIds = _fixture.CreateMany<Guid>(2).ToList();
        _sqlExecutorMock.Setup(e => e.QuerySingleValueAsync(_unitOfWorkMock.Object, QueryFiles.ExistsSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, Guid>>())).ReturnsAsync((Guid?)null);
        _sqlExecutorMock.Setup(e => e.QueryManyAsync(_unitOfWorkMock.Object, QueryFiles.GetPreviousIdsSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, Guid>>())).ReturnsAsync(previousIds);
        _sqlExecutorMock.Setup(e => e.QuerySingleAsync(_unitOfWorkMock.Object, QueryFiles.UpsertSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, Files>>())).ReturnsAsync(insertedFile);
        var queueMock = CaptureBackgroundTaskQueue();
        await CreateRepository(queueMock.Object).Upsert(_fixture.Create<UploadFileRequest>());

        await _capturedCallback!(CancellationToken.None);

        // one ExecuteAsync for the batched inactivate, one for the upsert
        _sessionMock.Verify(s => s.ExecuteAsync(It.IsAny<Statement>()), Times.Exactly(2));
    }

    [Test]
    public async Task Upsert_BackgroundUpdate_Should_SkipInactivateBatch_When_NoPreviousIds()
    {
        var insertedFile = _fixture.Create<Files>();
        _sqlExecutorMock.Setup(e => e.QuerySingleValueAsync(_unitOfWorkMock.Object, QueryFiles.ExistsSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, Guid>>())).ReturnsAsync((Guid?)null);
        _sqlExecutorMock.Setup(e => e.QueryManyAsync(_unitOfWorkMock.Object, QueryFiles.GetPreviousIdsSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, Guid>>())).ReturnsAsync([]);
        _sqlExecutorMock.Setup(e => e.QuerySingleAsync(_unitOfWorkMock.Object, QueryFiles.UpsertSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, Files>>())).ReturnsAsync(insertedFile);
        var queueMock = CaptureBackgroundTaskQueue();
        await CreateRepository(queueMock.Object).Upsert(_fixture.Create<UploadFileRequest>());

        await _capturedCallback!(CancellationToken.None);

        _sessionMock.Verify(s => s.ExecuteAsync(It.IsAny<Statement>()), Times.Once);
    }

    [Test]
    public async Task Update_BackgroundMetadataUpdate_Should_UpdateCql()
    {
        var currentFile = _fixture.Create<Files>();
        var updatedFile = _fixture.Create<Files>();
        _sqlExecutorMock.Setup(e => e.QuerySingleAsync(_unitOfWorkMock.Object, QueryFiles.GetByIdSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, Files>>())).ReturnsAsync(currentFile);
        _sqlExecutorMock.Setup(e => e.QuerySingleAsync(_unitOfWorkMock.Object, QueryFiles.UpdateSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, Files>>())).ReturnsAsync(updatedFile);
        var queueMock = CaptureBackgroundTaskQueue();
        await CreateRepository(queueMock.Object).Update(Guid.NewGuid(), _fixture.Create<UpdateFileRequest>());

        await _capturedCallback!(CancellationToken.None);

        _sessionMock.Verify(s => s.ExecuteAsync(It.IsAny<Statement>()), Times.Once);
    }

    [Test]
    public async Task Delete_BackgroundDelete_Should_DeleteCql()
    {
        var deletedFile = _fixture.Create<Files>();
        _sqlExecutorMock.Setup(e => e.QuerySingleAsync(QueryFiles.DeleteSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, Files>>())).ReturnsAsync(deletedFile);
        var queueMock = CaptureBackgroundTaskQueue();
        await CreateRepository(queueMock.Object).Delete(Guid.NewGuid());

        await _capturedCallback!(CancellationToken.None);

        _sessionMock.Verify(s => s.ExecuteAsync(It.IsAny<Statement>()), Times.Once);
    }

    [Test]
    public async Task DeleteHistoryBySourceMachineId_BackgroundBatchDelete_Should_DeleteCqlBatch()
    {
        var files = _fixture.CreateMany<Files>(3).ToList();
        _sqlExecutorMock.Setup(e => e.QueryManyAsync(QueryFiles.DeleteHistorySql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, Files>>())).ReturnsAsync(files);
        var queueMock = CaptureBackgroundTaskQueue();
        await CreateRepository(queueMock.Object).DeleteHistoryBySourceMachineId(1, "path");

        await _capturedCallback!(CancellationToken.None);

        // one batched ExecuteAsync covering all 3 files (batch size 100)
        _sessionMock.Verify(s => s.ExecuteAsync(It.IsAny<Statement>()), Times.Once);
    }

    [Test]
    public async Task BackgroundDelete_Should_HealScyllaSession_When_ConnectivityExceptionThrown()
    {
        _sessionMock.Setup(s => s.ExecuteAsync(It.IsAny<Statement>()))
            .ThrowsAsync(new NoHostAvailableException(new System.Collections.Generic.Dictionary<IPEndPoint, Exception>()));
        var sessionId = Guid.NewGuid();
        _scyllaProviderMock.Setup(p => p.GetCurrentSessionId()).Returns(sessionId);
        _scyllaProviderMock.Setup(p => p.HealSessionAsync(sessionId, It.IsAny<string>())).Returns(Task.CompletedTask);
        var deletedFile = _fixture.Create<Files>();
        _sqlExecutorMock.Setup(e => e.QuerySingleAsync(QueryFiles.DeleteSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, Files>>())).ReturnsAsync(deletedFile);
        var queueMock = CaptureBackgroundTaskQueue();
        await CreateRepository(queueMock.Object).Delete(Guid.NewGuid());

        await Should.ThrowAsync<NoHostAvailableException>(async () => await _capturedCallback!(CancellationToken.None));

        _scyllaProviderMock.Verify(p => p.HealSessionAsync(sessionId, It.IsAny<string>()), Times.Once);
    }

    [Test]
    public async Task BackgroundDelete_Should_Not_AttemptHeal_When_UnrelatedExceptionThrown()
    {
        _sessionMock.Setup(s => s.ExecuteAsync(It.IsAny<Statement>())).ThrowsAsync(new InvalidOperationException("boom"));
        var deletedFile = _fixture.Create<Files>();
        _sqlExecutorMock.Setup(e => e.QuerySingleAsync(QueryFiles.DeleteSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, Files>>())).ReturnsAsync(deletedFile);
        var queueMock = CaptureBackgroundTaskQueue();
        await CreateRepository(queueMock.Object).Delete(Guid.NewGuid());

        await Should.ThrowAsync<InvalidOperationException>(async () => await _capturedCallback!(CancellationToken.None));

        _scyllaProviderMock.Verify(p => p.HealSessionAsync(It.IsAny<Guid>(), It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task BackgroundDelete_Should_SwallowHealFailure_And_StillThrow_OriginalException()
    {
        _sessionMock.Setup(s => s.ExecuteAsync(It.IsAny<Statement>()))
            .ThrowsAsync(new NoHostAvailableException(new System.Collections.Generic.Dictionary<IPEndPoint, Exception>()));
        _scyllaProviderMock.Setup(p => p.HealSessionAsync(It.IsAny<Guid>(), It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException("heal failed"));
        var deletedFile = _fixture.Create<Files>();
        _sqlExecutorMock.Setup(e => e.QuerySingleAsync(QueryFiles.DeleteSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, Files>>())).ReturnsAsync(deletedFile);
        var queueMock = CaptureBackgroundTaskQueue();
        await CreateRepository(queueMock.Object).Delete(Guid.NewGuid());

        // the original NoHostAvailableException should still propagate, not the heal failure
        await Should.ThrowAsync<NoHostAvailableException>(async () => await _capturedCallback!(CancellationToken.None));
    }
}
