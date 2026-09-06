#nullable enable
using AutoFixture;
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
using ParameterNames = Media.Database.Repositories.Schemas.ParameterNames;

namespace Media.Database.Tests.Repositories;

/// <summary>
/// Covers FileRepository's public API against a mocked ISqlQueryExecutor. This is the payoff of
/// routing all Postgres access through ISqlQueryExecutor instead of opening a real NpgsqlConnection:
/// every branch (found/not-found, commit/rollback) is now testable. Scylla sync no longer happens
/// here at all -- it is Media.Worker's CDC pipeline's job, reading Postgres's write-ahead log.
/// </summary>
[TestFixture]
public class FileRepositoryQueryTests
{
    private Mock<ISqlQueryExecutor> _sqlExecutorMock = null!;
    private Mock<IUnitOfWork> _unitOfWorkMock = null!;
    private IFixture _fixture = null!;

    [SetUp]
    public void Setup()
    {
        _fixture = AutoMoqFixture.Create();
        _sqlExecutorMock = new Mock<ISqlQueryExecutor>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
    }

    private FileRepository CreateRepository()
    {
        return new FileRepository(
            _sqlExecutorMock.Object,
            () => _unitOfWorkMock.Object,
            Mock.Of<IMapChangeWordRequests>(),
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
    public async Task Delete_Should_ReturnFile_When_ExecutorFindsMatch()
    {
        var expected = _fixture.Create<Files>();
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(QueryFiles.DeleteSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, Files>>()))
            .ReturnsAsync(expected);

        var result = await CreateRepository().Delete(Guid.NewGuid());

        result.ShouldBe(expected);
    }

    [Test]
    public async Task Delete_Should_ReturnNull_When_ExecutorFindsNoMatch()
    {
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(QueryFiles.DeleteSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, Files>>()))
            .ReturnsAsync((Files?)null);

        var result = await CreateRepository().Delete(Guid.NewGuid());

        result.ShouldBeNull();
    }

    [Test]
    public async Task DeleteHistoryBySourceMachineId_Should_ReturnFiles_When_FilesFound()
    {
        var files = _fixture.CreateMany<Files>(2).ToList();
        _sqlExecutorMock
            .Setup(e => e.QueryManyAsync(QueryFiles.DeleteHistorySql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, Files>>()))
            .ReturnsAsync(files);

        var result = await CreateRepository().DeleteHistoryBySourceMachineId(1, "path");

        result.ShouldBe(files);
    }

    [Test]
    public async Task DeleteHistoryBySourceMachineId_Should_ReturnEmpty_When_NoFilesFound()
    {
        _sqlExecutorMock
            .Setup(e => e.QueryManyAsync(QueryFiles.DeleteHistorySql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, Files>>()))
            .ReturnsAsync([]);

        var result = await CreateRepository().DeleteHistoryBySourceMachineId(1, "path");

        result.ShouldBeEmpty();
    }

    [Test]
    public async Task Upsert_Should_ReturnExistingFile_And_Rollback_When_FileAlreadyExists()
    {
        var existingId = Guid.NewGuid();
        _sqlExecutorMock
            .Setup(e => e.QuerySingleValueAsync(_unitOfWorkMock.Object, QueryFiles.ExistsSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, Guid>>()))
            .ReturnsAsync(existingId);
        var request = _fixture.Create<UploadFileRequest>();
        var sourceMachineId = _fixture.Create<int>();

        var result = await CreateRepository().Upsert(sourceMachineId, request);

        result.ShouldNotBeNull();
        result!.Id.ShouldBe(existingId);
        result.Exists.ShouldBeTrue();
        _unitOfWorkMock.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
        _sqlExecutorMock.Verify(e => e.QueryManyAsync(_unitOfWorkMock.Object, QueryFiles.GetPreviousIdsSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, Guid>>()), Times.Never);
    }

    [Test]
    public async Task Upsert_Should_Commit_When_NewFileInserted()
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
        var sourceMachineId = _fixture.Create<int>();

        var result = await CreateRepository().Upsert(sourceMachineId, request);

        result.ShouldBe(insertedFile);
        _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Never);
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
        var sourceMachineId = _fixture.Create<int>();

        var result = await CreateRepository().Upsert(sourceMachineId, request);

        result.ShouldBeNull();
        _unitOfWorkMock.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task Upsert_Should_ConfigureLastFileUpdateAndMetadata_When_BothProvided()
    {
        var insertedFile = _fixture.Create<Files>();
        Action<NpgsqlParameterCollection>? existsCaptured = null;
        _sqlExecutorMock
            .Setup(e => e.QuerySingleValueAsync(_unitOfWorkMock.Object, QueryFiles.ExistsSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, Guid>>()))
            .Callback<IUnitOfWork, string, Action<NpgsqlParameterCollection>, Func<NpgsqlDataReader, Guid>>((_, _, configure, _) => existsCaptured = configure)
            .ReturnsAsync((Guid?)null);
        _sqlExecutorMock
            .Setup(e => e.QueryManyAsync(_unitOfWorkMock.Object, QueryFiles.GetPreviousIdsSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, Guid>>()))
            .ReturnsAsync([]);
        Action<NpgsqlParameterCollection>? upsertCaptured = null;
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(_unitOfWorkMock.Object, QueryFiles.UpsertSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, Files>>()))
            .Callback<IUnitOfWork, string, Action<NpgsqlParameterCollection>, Func<NpgsqlDataReader, Files>>((_, _, configure, _) => upsertCaptured = configure)
            .ReturnsAsync(insertedFile);
        var request = _fixture.Build<UploadFileRequest>()
            .With(r => r.LastFileUpdate, DateTimeOffset.UtcNow)
            .With(r => r.Metadata, new Metadata { Title = "a title" })
            .Create();
        var sourceMachineId = _fixture.Create<int>();

        await CreateRepository().Upsert(sourceMachineId, request);

        using var existsCommand = new NpgsqlCommand();
        existsCaptured!(existsCommand.Parameters);
        existsCommand.Parameters[ParameterNames.LastFileUpdate].Value.ShouldBe(request.LastFileUpdate);

        using var upsertCommand = new NpgsqlCommand();
        upsertCaptured!(upsertCommand.Parameters);
        upsertCommand.Parameters[ParameterNames.Metadata].Value.ShouldNotBe(DBNull.Value);
    }

    [Test]
    public async Task Upsert_Should_ConfigureLastFileUpdateAndMetadata_As_DbNull_When_BothNull()
    {
        var insertedFile = _fixture.Create<Files>();
        Action<NpgsqlParameterCollection>? existsCaptured = null;
        _sqlExecutorMock
            .Setup(e => e.QuerySingleValueAsync(_unitOfWorkMock.Object, QueryFiles.ExistsSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, Guid>>()))
            .Callback<IUnitOfWork, string, Action<NpgsqlParameterCollection>, Func<NpgsqlDataReader, Guid>>((_, _, configure, _) => existsCaptured = configure)
            .ReturnsAsync((Guid?)null);
        _sqlExecutorMock
            .Setup(e => e.QueryManyAsync(_unitOfWorkMock.Object, QueryFiles.GetPreviousIdsSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, Guid>>()))
            .ReturnsAsync([]);
        Action<NpgsqlParameterCollection>? upsertCaptured = null;
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(_unitOfWorkMock.Object, QueryFiles.UpsertSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, Files>>()))
            .Callback<IUnitOfWork, string, Action<NpgsqlParameterCollection>, Func<NpgsqlDataReader, Files>>((_, _, configure, _) => upsertCaptured = configure)
            .ReturnsAsync(insertedFile);
        var request = _fixture.Build<UploadFileRequest>()
            .With(r => r.LastFileUpdate, (DateTimeOffset?)null)
            .With(r => r.Metadata, (Metadata?)null)
            .Create();
        var sourceMachineId = _fixture.Create<int>();

        await CreateRepository().Upsert(sourceMachineId, request);

        using var existsCommand = new NpgsqlCommand();
        existsCaptured!(existsCommand.Parameters);
        existsCommand.Parameters[ParameterNames.LastFileUpdate].Value.ShouldBe(DBNull.Value);

        using var upsertCommand = new NpgsqlCommand();
        upsertCaptured!(upsertCommand.Parameters);
        upsertCommand.Parameters[ParameterNames.Metadata].Value.ShouldBe(DBNull.Value);
    }

    [Test]
    public void Upsert_Should_RollbackAndRethrow_When_ExecutorThrows()
    {
        _unitOfWorkMock.Setup(u => u.CurrentTransaction).Returns((Npgsql.NpgsqlTransaction)null!);
        _sqlExecutorMock
            .Setup(e => e.QuerySingleValueAsync(_unitOfWorkMock.Object, QueryFiles.ExistsSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, Guid>>()))
            .ThrowsAsync(new InvalidOperationException("boom"));
        var request = _fixture.Create<UploadFileRequest>();
        var sourceMachineId = _fixture.Create<int>();

        Should.ThrowAsync<InvalidOperationException>(() => CreateRepository().Upsert(sourceMachineId, request));
    }

    [Test]
    public void Upsert_Should_Rollback_When_ExecutorThrows_And_TransactionIsActive()
    {
        _unitOfWorkMock.Setup(u => u.CurrentTransaction).Returns((NpgsqlTransaction)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(NpgsqlTransaction)));
        _sqlExecutorMock
            .Setup(e => e.QuerySingleValueAsync(_unitOfWorkMock.Object, QueryFiles.ExistsSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, Guid>>()))
            .ThrowsAsync(new InvalidOperationException("boom"));
        var request = _fixture.Create<UploadFileRequest>();
        var sourceMachineId = _fixture.Create<int>();

        Should.ThrowAsync<InvalidOperationException>(() => CreateRepository().Upsert(sourceMachineId, request));

        _unitOfWorkMock.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public void Update_Should_RollbackAndRethrow_When_ExecutorThrows_And_TransactionIsActive()
    {
        _unitOfWorkMock.Setup(u => u.CurrentTransaction).Returns((NpgsqlTransaction)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(NpgsqlTransaction)));
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(_unitOfWorkMock.Object, QueryFiles.GetByIdSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, Files>>()))
            .ThrowsAsync(new InvalidOperationException("boom"));
        var request = _fixture.Create<UpdateFileRequest>();

        Should.ThrowAsync<InvalidOperationException>(() => CreateRepository().Update(Guid.NewGuid(), request));

        _unitOfWorkMock.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public void Update_Should_Rethrow_Without_Rollback_When_ExecutorThrows_And_NoTransactionActive()
    {
        _unitOfWorkMock.Setup(u => u.CurrentTransaction).Returns((NpgsqlTransaction)null!);
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(_unitOfWorkMock.Object, QueryFiles.GetByIdSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, Files>>()))
            .ThrowsAsync(new InvalidOperationException("boom"));
        var request = _fixture.Create<UpdateFileRequest>();

        Should.ThrowAsync<InvalidOperationException>(() => CreateRepository().Update(Guid.NewGuid(), request));

        _unitOfWorkMock.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Never);
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
    public async Task Update_Should_Commit_When_FileUpdated()
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
    }

    [Test]
    public async Task Update_Should_ConfigureMetadataAsJson_When_MetadataProvided()
    {
        var currentFile = _fixture.Create<Files>();
        var updatedFile = _fixture.Create<Files>();
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(_unitOfWorkMock.Object, QueryFiles.GetByIdSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, Files>>()))
            .ReturnsAsync(currentFile);
        Action<NpgsqlParameterCollection>? captured = null;
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(_unitOfWorkMock.Object, QueryFiles.UpdateSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, Files>>()))
            .Callback<IUnitOfWork, string, Action<NpgsqlParameterCollection>, Func<NpgsqlDataReader, Files>>((_, _, configure, _) => captured = configure)
            .ReturnsAsync(updatedFile);
        var request = _fixture.Build<UpdateFileRequest>()
            .With(r => r.Metadata, new Metadata { Title = "a title" })
            .Create();

        await CreateRepository().Update(Guid.NewGuid(), request);

        using var command = new NpgsqlCommand();
        captured!(command.Parameters);
        command.Parameters[ParameterNames.Metadata].Value.ShouldNotBe(DBNull.Value);
    }

    [Test]
    public async Task Update_Should_ConfigureMetadataAsDbNull_When_MetadataNull()
    {
        var currentFile = _fixture.Create<Files>();
        var updatedFile = _fixture.Create<Files>();
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(_unitOfWorkMock.Object, QueryFiles.GetByIdSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, Files>>()))
            .ReturnsAsync(currentFile);
        Action<NpgsqlParameterCollection>? captured = null;
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(_unitOfWorkMock.Object, QueryFiles.UpdateSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, Files>>()))
            .Callback<IUnitOfWork, string, Action<NpgsqlParameterCollection>, Func<NpgsqlDataReader, Files>>((_, _, configure, _) => captured = configure)
            .ReturnsAsync(updatedFile);
        var request = _fixture.Build<UpdateFileRequest>()
            .With(r => r.Metadata, (Metadata?)null)
            .Create();

        await CreateRepository().Update(Guid.NewGuid(), request);

        using var command = new NpgsqlCommand();
        captured!(command.Parameters);
        command.Parameters[ParameterNames.Metadata].Value.ShouldBe(DBNull.Value);
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
    }
}
