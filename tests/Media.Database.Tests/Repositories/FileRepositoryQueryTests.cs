#nullable enable
using AutoFixture;
using Media.Common.Providers;
using Media.Common.Transactions;
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
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using ParameterNames = Media.Database.Repositories.Schemas.ParameterNames;

namespace Media.Database.Tests.Repositories;

/// <summary>
/// Covers FileRepository's public API against a mocked ISqlQueryExecutor (and, for the Scylla-first
/// read path, a mocked ICqlQueryExecutor). This is the payoff of routing all Postgres/Scylla access
/// through executor interfaces instead of opening real connections: every branch (found/not-found,
/// commit/rollback, Scylla-hit/miss/failure) is now testable. Scylla *writes* still happen entirely
/// via Media.Worker's CDC pipeline, reading Postgres's write-ahead log -- this repository never
/// writes to Scylla, only reads from it as an optimization with a Postgres fallback.
/// </summary>
[TestFixture]
public class FileRepositoryQueryTests
{
    private Mock<ISqlQueryExecutor> _sqlExecutorMock = null!;
    private Mock<ICqlQueryExecutor> _cqlExecutorMock = null!;
    private Mock<IScyllaSessionProvider> _scyllaProviderMock = null!;
    private Mock<IUnitOfWork> _unitOfWorkMock = null!;
    private IFixture _fixture = null!;

    [SetUp]
    public void Setup()
    {
        _fixture = AutoMoqFixture.Create();
        _sqlExecutorMock = new Mock<ISqlQueryExecutor>();
        _cqlExecutorMock = new Mock<ICqlQueryExecutor>();
        _scyllaProviderMock = new Mock<IScyllaSessionProvider>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
    }

    private FileRepository CreateRepository()
    {
        return new FileRepository(
            _sqlExecutorMock.Object,
            _cqlExecutorMock.Object,
            _scyllaProviderMock.Object,
            () => _unitOfWorkMock.Object,
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
    public async Task GetCurrentPageIdentifiersBySourceMachineId_Should_ReturnIdentifiers_From_Executor()
    {
        var expected = new List<(Guid Id, string OriginalFilePath)> { (Id: Guid.NewGuid(), OriginalFilePath: "path") };
        _sqlExecutorMock
            .Setup(e => e.QueryManyAsync(QueryFiles.GetCurrentPageIdentifiersBySourceMachineIdSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, (Guid Id, string OriginalFilePath)>>()))
            .ReturnsAsync(expected);

        var result = await CreateRepository().GetCurrentPageIdentifiersBySourceMachineId(1, "path");

        result.ShouldBe(expected);
    }

    [Test]
    public async Task GetByIds_Should_PreferScylla_When_RowFound()
    {
        var id = Guid.NewGuid();
        var scyllaFile = new Files { Id = id, OriginalFilePath = "from-scylla" };
        _cqlExecutorMock
            .Setup(e => e.QuerySingleAsync(QueryFiles.GetByIdCql, It.IsAny<Action<Dictionary<string, object>>>(), It.IsAny<Func<Cassandra.Row, Files>>()))
            .ReturnsAsync(scyllaFile);

        var result = await CreateRepository().GetByIds([id], maxDegreeOfParallelism: 1);

        result.ShouldBe([scyllaFile]);
        _sqlExecutorMock.Verify(e => e.QuerySingleAsync(QueryFiles.GetByIdSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, Files>>()), Times.Never);
    }

    [Test]
    public async Task GetByIds_Should_FallBackToPostgres_When_ScyllaHasNoRowYet()
    {
        var id = Guid.NewGuid();
        var postgresFile = new Files { Id = id, OriginalFilePath = "from-postgres" };
        _cqlExecutorMock
            .Setup(e => e.QuerySingleAsync(QueryFiles.GetByIdCql, It.IsAny<Action<Dictionary<string, object>>>(), It.IsAny<Func<Cassandra.Row, Files>>()))
            .ReturnsAsync((Files?)null);
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(QueryFiles.GetByIdSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, Files>>()))
            .ReturnsAsync(postgresFile);

        var result = await CreateRepository().GetByIds([id], maxDegreeOfParallelism: 1);

        result.ShouldBe([postgresFile]);
    }

    [Test]
    public async Task GetByIds_Should_FallBackToPostgres_And_AttemptHeal_When_ScyllaConnectivityExceptionThrown()
    {
        var id = Guid.NewGuid();
        var postgresFile = new Files { Id = id, OriginalFilePath = "from-postgres" };
        _cqlExecutorMock
            .Setup(e => e.QuerySingleAsync(QueryFiles.GetByIdCql, It.IsAny<Action<Dictionary<string, object>>>(), It.IsAny<Func<Cassandra.Row, Files>>()))
            .ThrowsAsync(new Cassandra.NoHostAvailableException(new Dictionary<IPEndPoint, Exception>()));
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(QueryFiles.GetByIdSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, Files>>()))
            .ReturnsAsync(postgresFile);
        _scyllaProviderMock.Setup(p => p.GetCurrentSessionId()).Returns(Guid.NewGuid());

        var result = await CreateRepository().GetByIds([id], maxDegreeOfParallelism: 1);

        result.ShouldBe([postgresFile]);
        _scyllaProviderMock.Verify(p => p.HealSessionAsync(It.IsAny<Guid>(), It.IsAny<string?>()), Times.Once);
    }

    [Test]
    public async Task GetByIds_Should_FallBackToPostgres_When_ScyllaThrowsNonConnectivityException()
    {
        var id = Guid.NewGuid();
        var postgresFile = new Files { Id = id, OriginalFilePath = "from-postgres" };
        _cqlExecutorMock
            .Setup(e => e.QuerySingleAsync(QueryFiles.GetByIdCql, It.IsAny<Action<Dictionary<string, object>>>(), It.IsAny<Func<Cassandra.Row, Files>>()))
            .ThrowsAsync(new InvalidOperationException("boom"));
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(QueryFiles.GetByIdSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, Files>>()))
            .ReturnsAsync(postgresFile);

        var result = await CreateRepository().GetByIds([id], maxDegreeOfParallelism: 1);

        result.ShouldBe([postgresFile]);
        _scyllaProviderMock.Verify(p => p.HealSessionAsync(It.IsAny<Guid>(), It.IsAny<string?>()), Times.Never);
    }

    [Test]
    public async Task GetByIds_Should_OmitId_When_NeitherStoreHasIt()
    {
        var id = Guid.NewGuid();
        _cqlExecutorMock
            .Setup(e => e.QuerySingleAsync(QueryFiles.GetByIdCql, It.IsAny<Action<Dictionary<string, object>>>(), It.IsAny<Func<Cassandra.Row, Files>>()))
            .ReturnsAsync((Files?)null);
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(QueryFiles.GetByIdSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, Files>>()))
            .ReturnsAsync((Files?)null);

        var result = await CreateRepository().GetByIds([id], maxDegreeOfParallelism: 1);

        result.ShouldBeEmpty();
    }

    [Test]
    public async Task GetByIds_Should_HydrateEveryId_When_MultipleIdsRequested()
    {
        var ids = _fixture.CreateMany<Guid>(4).ToList();
        _cqlExecutorMock
            .Setup(e => e.QuerySingleAsync(QueryFiles.GetByIdCql, It.IsAny<Action<Dictionary<string, object>>>(), It.IsAny<Func<Cassandra.Row, Files>>()))
            .ReturnsAsync((string _, Action<Dictionary<string, object>> configure, Func<Cassandra.Row, Files> _) =>
            {
                var parameters = new Dictionary<string, object>();
                configure(parameters);
                var id = (Guid)parameters[ParameterNames.Id.ToUpperInvariant()];
                return new Files { Id = id, OriginalFilePath = id.ToString() };
            });

        var result = await CreateRepository().GetByIds(ids, maxDegreeOfParallelism: 2);

        result.Select(f => f.Id).ShouldBe(ids, ignoreOrder: true);
    }

    [Test]
    public async Task SetThumbnailGeneratedOn_Should_Execute_SetThumbnailGeneratedOnSql()
    {
        var id = Guid.NewGuid();
        var generatedOn = _fixture.Create<DateTimeOffset>();
        Action<NpgsqlParameterCollection>? captured = null;
        _sqlExecutorMock
            .Setup(e => e.ExecuteAsync(QueryFiles.SetThumbnailGeneratedOnSql, It.IsAny<Action<NpgsqlParameterCollection>>()))
            .Callback<string, Action<NpgsqlParameterCollection>>((_, configure) => captured = configure)
            .ReturnsAsync(1);

        await CreateRepository().SetThumbnailGeneratedOn(id, generatedOn);

        _sqlExecutorMock.Verify(e => e.ExecuteAsync(QueryFiles.SetThumbnailGeneratedOnSql, It.IsAny<Action<NpgsqlParameterCollection>>()), Times.Once);
        using var command = new NpgsqlCommand();
        captured!(command.Parameters);
        command.Parameters[ParameterNames.Id].Value.ShouldBe(id);
        command.Parameters[ParameterNames.ThumbnailGeneratedOn].Value.ShouldBe(generatedOn);
    }

    [Test]
    public void SetThumbnailGeneratedOn_Should_Rethrow_When_ExecutorThrows()
    {
        _sqlExecutorMock
            .Setup(e => e.ExecuteAsync(QueryFiles.SetThumbnailGeneratedOnSql, It.IsAny<Action<NpgsqlParameterCollection>>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        Should.ThrowAsync<InvalidOperationException>(() => CreateRepository().SetThumbnailGeneratedOn(Guid.NewGuid(), DateTimeOffset.UtcNow));
    }

    [Test]
    public async Task RefreshView_Should_Execute_RefreshViewSql()
    {
        await CreateRepository().RefreshView();

        _sqlExecutorMock.Verify(e => e.ExecuteAsync(QueryFiles.RefreshViewSql, It.IsAny<Action<NpgsqlParameterCollection>>()), Times.Once);
    }

    [Test]
    public void RefreshView_Should_Rethrow_When_ExecutorThrows()
    {
        _sqlExecutorMock
            .Setup(e => e.ExecuteAsync(QueryFiles.RefreshViewSql, It.IsAny<Action<NpgsqlParameterCollection>>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        Should.ThrowAsync<InvalidOperationException>(() => CreateRepository().RefreshView());
    }

    [Test]
    public async Task GetHistoryPagesBySourceMachineId_Should_ReturnFiles_HydratedFromScylla()
    {
        var files = _fixture.CreateMany<Files>(2).ToList();
        var ids = files.Select(f => f.Id).ToList();
        _sqlExecutorMock
            .Setup(e => e.QueryManyAsync(QueryFiles.GetHistoryIdsBySourceMachineIdSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, Guid>>()))
            .ReturnsAsync(ids);
        _cqlExecutorMock
            .Setup(e => e.QuerySingleAsync(QueryFiles.GetByIdCql, It.IsAny<Action<Dictionary<string, object>>>(), It.IsAny<Func<Cassandra.Row, Files>>()))
            .ReturnsAsync((string _, Action<Dictionary<string, object>> configure, Func<Cassandra.Row, Files> _) =>
            {
                var parameters = new Dictionary<string, object>();
                configure(parameters);
                var id = (Guid)parameters[ParameterNames.Id.ToUpperInvariant()];
                return files.First(f => f.Id == id);
            });

        var result = await CreateRepository().GetHistoryPagesBySourceMachineId(1, "path");

        result.ShouldBe(files, ignoreOrder: true);
    }

    [Test]
    public async Task GetHistoryPagesBySourceMachineId_Should_ReturnEmpty_When_NoIdsFound()
    {
        _sqlExecutorMock
            .Setup(e => e.QueryManyAsync(QueryFiles.GetHistoryIdsBySourceMachineIdSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, Guid>>()))
            .ReturnsAsync([]);

        var result = await CreateRepository().GetHistoryPagesBySourceMachineId(1, "path");

        result.ShouldBeEmpty();
        _cqlExecutorMock.Verify(e => e.QuerySingleAsync(QueryFiles.GetByIdCql, It.IsAny<Action<Dictionary<string, object>>>(), It.IsAny<Func<Cassandra.Row, Files>>()), Times.Never);
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
            .Setup(e => e.QuerySingleAsync(_unitOfWorkMock.Object, QueryFiles.UpdateSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, Files>>()))
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
            .Setup(e => e.QuerySingleAsync(_unitOfWorkMock.Object, QueryFiles.UpdateSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, Files>>()))
            .ThrowsAsync(new InvalidOperationException("boom"));
        var request = _fixture.Create<UpdateFileRequest>();

        Should.ThrowAsync<InvalidOperationException>(() => CreateRepository().Update(Guid.NewGuid(), request));

        _unitOfWorkMock.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task Update_Should_Commit_When_FileUpdated()
    {
        var updatedFile = _fixture.Create<Files>();
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
        var updatedFile = _fixture.Create<Files>();
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
        var updatedFile = _fixture.Create<Files>();
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
