#nullable enable
using AutoFixture;
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
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Media.Database.Tests.Repositories;

/// <summary>
/// Covers FileRepository's public API against a mocked ISqlQueryExecutor. This is the payoff of
/// routing all Postgres access through ISqlQueryExecutor instead of opening a real NpgsqlConnection:
/// every branch (found/not-found, commit/rollback, background-queue triggers) is now testable.
/// </summary>
[TestFixture]
public class FileRepositoryQueryTests
{
    private Mock<ISqlQueryExecutor> _sqlExecutorMock = null!;
    private Mock<IBackgroundTaskQueue> _backgroundTaskQueueMock = null!;
    private Mock<IUnitOfWork> _unitOfWorkMock = null!;
    private IFixture _fixture = null!;

    [SetUp]
    public void Setup()
    {
        _fixture = AutoMoqFixture.Create();
        _sqlExecutorMock = new Mock<ISqlQueryExecutor>();
        _backgroundTaskQueueMock = new Mock<IBackgroundTaskQueue>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
    }

    private FileRepository CreateRepository()
    {
        var scyllaProviderMock = new Mock<IScyllaSessionProvider>();
        scyllaProviderMock.Setup(p => p.MaxBatchSize).Returns(100);

        return new FileRepository(
            _sqlExecutorMock.Object,
            scyllaProviderMock.Object,
            () => _unitOfWorkMock.Object,
            Mock.Of<IMapChangeWordRequests>(),
            _backgroundTaskQueueMock.Object,
            Mock.Of<ILogger<FileRepository>>(),
            new LoggingLevelSwitch());
    }

    [Test]
    public async Task GetById_Should_ReturnFile_When_ExecutorFindsMatch()
    {
        var expected = _fixture.Create<Files>();
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(QueryFiles.GetByIdSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, Files>>()))
            .ReturnsAsync(expected);

        var result = await CreateRepository().GetById(Guid.NewGuid());

        result.ShouldBe(expected);
    }

    [Test]
    public async Task GetById_Should_ReturnNull_When_ExecutorFindsNoMatch()
    {
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(QueryFiles.GetByIdSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, Files>>()))
            .ReturnsAsync((Files?)null);

        var result = await CreateRepository().GetById(Guid.NewGuid());

        result.ShouldBeNull();
    }

    [Test]
    public async Task GetById_Should_ConfigureIdParameter()
    {
        Action<NpgsqlParameterCollection>? captured = null;
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(QueryFiles.GetByIdSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, Files>>()))
            .Callback<string, Action<NpgsqlParameterCollection>, Func<NpgsqlDataReader, Files>>((_, configure, _) => captured = configure)
            .ReturnsAsync((Files?)null);
        var id = Guid.NewGuid();

        await CreateRepository().GetById(id);

        using var command = new NpgsqlCommand();
        captured!(command.Parameters);
        command.Parameters[Media.Database.Repositories.Schemas.ParameterNames.Id].Value.ShouldBe(id);
    }

    [Test]
    public async Task GetCurrentBySourceMachineId_Should_ReturnFile_When_ExecutorFindsMatch()
    {
        var expected = _fixture.Create<Files>();
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(QueryFiles.GetCurrentBySourceMachineIdSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, Files>>()))
            .ReturnsAsync(expected);

        var result = await CreateRepository().GetCurrentBySourceMachineId(1, "path");

        result.ShouldBe(expected);
    }

    [Test]
    public async Task GetCurrentPagesBySourceMachineId_Should_ReturnFiles_From_Executor()
    {
        var expected = _fixture.CreateMany<Files>(3).ToList();
        _sqlExecutorMock
            .Setup(e => e.QueryManyAsync(QueryFiles.GetCurrentPagesBySourceMachineIdSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, Files>>()))
            .ReturnsAsync(expected);

        var result = await CreateRepository().GetCurrentPagesBySourceMachineId(1, "path");

        result.ShouldBe(expected);
    }

    [Test]
    public async Task GetHistoryPagesBySourceMachineId_Should_ReturnFiles_From_Executor()
    {
        var expected = _fixture.CreateMany<Files>(2).ToList();
        _sqlExecutorMock
            .Setup(e => e.QueryManyAsync(QueryFiles.GetHistoryPagesBySourceMachineIdSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, Files>>()))
            .ReturnsAsync(expected);

        var result = await CreateRepository().GetHistoryPagesBySourceMachineId(1, "path");

        result.ShouldBe(expected);
    }

    [Test]
    public async Task Delete_Should_ReturnFile_And_QueueBackgroundDelete_When_ExecutorFindsMatch()
    {
        var expected = _fixture.Create<Files>();
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(QueryFiles.DeleteSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, Files>>()))
            .ReturnsAsync(expected);

        var result = await CreateRepository().Delete(Guid.NewGuid());

        result.ShouldBe(expected);
        _backgroundTaskQueueMock.Verify(q => q.QueueBackgroundWorkItemAsync(It.IsAny<Func<CancellationToken, ValueTask>>()), Times.Once);
    }

    [Test]
    public async Task Delete_Should_ReturnNull_And_NotQueueBackgroundDelete_When_ExecutorFindsNoMatch()
    {
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(QueryFiles.DeleteSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, Files>>()))
            .ReturnsAsync((Files?)null);

        var result = await CreateRepository().Delete(Guid.NewGuid());

        result.ShouldBeNull();
        _backgroundTaskQueueMock.Verify(q => q.QueueBackgroundWorkItemAsync(It.IsAny<Func<CancellationToken, ValueTask>>()), Times.Never);
    }

    [Test]
    public async Task DeleteHistoryBySourceMachineId_Should_QueueBackgroundDelete_When_FilesFound()
    {
        var files = _fixture.CreateMany<Files>(2).ToList();
        _sqlExecutorMock
            .Setup(e => e.QueryManyAsync(QueryFiles.DeleteHistorySql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, Files>>()))
            .ReturnsAsync(files);

        var result = await CreateRepository().DeleteHistoryBySourceMachineId(1, "path");

        result.ShouldBe(files);
        _backgroundTaskQueueMock.Verify(q => q.QueueBackgroundWorkItemAsync(It.IsAny<Func<CancellationToken, ValueTask>>()), Times.Once);
    }

    [Test]
    public async Task DeleteHistoryBySourceMachineId_Should_NotQueueBackgroundDelete_When_NoFilesFound()
    {
        _sqlExecutorMock
            .Setup(e => e.QueryManyAsync(QueryFiles.DeleteHistorySql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, Files>>()))
            .ReturnsAsync([]);

        var result = await CreateRepository().DeleteHistoryBySourceMachineId(1, "path");

        result.ShouldBeEmpty();
        _backgroundTaskQueueMock.Verify(q => q.QueueBackgroundWorkItemAsync(It.IsAny<Func<CancellationToken, ValueTask>>()), Times.Never);
    }

    [Test]
    public async Task Upsert_Should_ReturnExistingFile_And_Rollback_When_FileAlreadyExists()
    {
        var existingId = Guid.NewGuid();
        _sqlExecutorMock
            .Setup(e => e.QuerySingleValueAsync(_unitOfWorkMock.Object, QueryFiles.ExistsSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, Guid>>()))
            .ReturnsAsync(existingId);
        var request = _fixture.Create<UploadFileRequest>();

        var result = await CreateRepository().Upsert(request);

        result.ShouldNotBeNull();
        result!.Id.ShouldBe(existingId);
        result.Exists.ShouldBeTrue();
        _unitOfWorkMock.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
        _sqlExecutorMock.Verify(e => e.QueryManyAsync(_unitOfWorkMock.Object, QueryFiles.GetPreviousIdsSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, Guid>>()), Times.Never);
    }

    [Test]
    public async Task Upsert_Should_CommitAndQueueBackgroundUpdate_When_NewFileInserted()
    {
        var insertedFile = _fixture.Create<Files>();
        var previousIds = _fixture.CreateMany<Guid>(2).ToList();
        _sqlExecutorMock
            .Setup(e => e.QuerySingleValueAsync(_unitOfWorkMock.Object, QueryFiles.ExistsSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, Guid>>()))
            .ReturnsAsync((Guid?)null);
        _sqlExecutorMock
            .Setup(e => e.QueryManyAsync(_unitOfWorkMock.Object, QueryFiles.GetPreviousIdsSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, Guid>>()))
            .ReturnsAsync(previousIds);
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(_unitOfWorkMock.Object, QueryFiles.UpsertSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, Files>>()))
            .ReturnsAsync(insertedFile);
        var request = _fixture.Create<UploadFileRequest>();

        var result = await CreateRepository().Upsert(request);

        result.ShouldBe(insertedFile);
        _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Never);
        _backgroundTaskQueueMock.Verify(q => q.QueueBackgroundWorkItemAsync(It.IsAny<Func<CancellationToken, ValueTask>>()), Times.Once);
    }

    [Test]
    public async Task Upsert_Should_ReturnNull_And_Rollback_When_InsertReturnsNoRow()
    {
        _sqlExecutorMock
            .Setup(e => e.QuerySingleValueAsync(_unitOfWorkMock.Object, QueryFiles.ExistsSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, Guid>>()))
            .ReturnsAsync((Guid?)null);
        _sqlExecutorMock
            .Setup(e => e.QueryManyAsync(_unitOfWorkMock.Object, QueryFiles.GetPreviousIdsSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, Guid>>()))
            .ReturnsAsync([]);
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(_unitOfWorkMock.Object, QueryFiles.UpsertSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, Files>>()))
            .ReturnsAsync((Files?)null);
        var request = _fixture.Create<UploadFileRequest>();

        var result = await CreateRepository().Upsert(request);

        result.ShouldBeNull();
        _unitOfWorkMock.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
        _backgroundTaskQueueMock.Verify(q => q.QueueBackgroundWorkItemAsync(It.IsAny<Func<CancellationToken, ValueTask>>()), Times.Never);
    }

    [Test]
    public void Upsert_Should_RollbackAndRethrow_When_ExecutorThrows()
    {
        _unitOfWorkMock.Setup(u => u.CurrentTransaction).Returns((Npgsql.NpgsqlTransaction)null!);
        _sqlExecutorMock
            .Setup(e => e.QuerySingleValueAsync(_unitOfWorkMock.Object, QueryFiles.ExistsSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, Guid>>()))
            .ThrowsAsync(new InvalidOperationException("boom"));
        var request = _fixture.Create<UploadFileRequest>();

        Should.ThrowAsync<InvalidOperationException>(() => CreateRepository().Upsert(request));
    }

    [Test]
    public async Task Update_Should_ReturnNullFile_And_Rollback_When_FileNotFound()
    {
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(_unitOfWorkMock.Object, QueryFiles.GetByIdSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, Files>>()))
            .ReturnsAsync((Files?)null);
        var request = _fixture.Create<UpdateFileRequest>();

        var response = await CreateRepository().Update(Guid.NewGuid(), request);

        response.File.ShouldBeNull();
        _unitOfWorkMock.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task Update_Should_CommitAndQueueBackgroundMetadataUpdate_When_FileUpdated()
    {
        var currentFile = _fixture.Create<Files>();
        var updatedFile = _fixture.Create<Files>();
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(_unitOfWorkMock.Object, QueryFiles.GetByIdSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, Files>>()))
            .ReturnsAsync(currentFile);
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(_unitOfWorkMock.Object, QueryFiles.UpdateSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, Files>>()))
            .ReturnsAsync(updatedFile);
        var request = _fixture.Create<UpdateFileRequest>();

        var response = await CreateRepository().Update(Guid.NewGuid(), request);

        response.File.ShouldBe(updatedFile);
        _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        _backgroundTaskQueueMock.Verify(q => q.QueueBackgroundWorkItemAsync(It.IsAny<Func<CancellationToken, ValueTask>>()), Times.Once);
    }

    [Test]
    public async Task Update_Should_ReturnNullFile_And_Rollback_When_UpdateSql_ReturnsNoRow()
    {
        var currentFile = _fixture.Create<Files>();
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(_unitOfWorkMock.Object, QueryFiles.GetByIdSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, Files>>()))
            .ReturnsAsync(currentFile);
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(_unitOfWorkMock.Object, QueryFiles.UpdateSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, Files>>()))
            .ReturnsAsync((Files?)null);
        var request = _fixture.Create<UpdateFileRequest>();

        var response = await CreateRepository().Update(Guid.NewGuid(), request);

        response.File.ShouldBeNull();
        _unitOfWorkMock.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
        _backgroundTaskQueueMock.Verify(q => q.QueueBackgroundWorkItemAsync(It.IsAny<Func<CancellationToken, ValueTask>>()), Times.Never);
    }
}
