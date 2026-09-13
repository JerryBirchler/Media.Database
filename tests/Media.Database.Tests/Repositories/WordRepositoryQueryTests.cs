#nullable enable
using AutoFixture;
using Media.Database.Models;
using Media.Database.Repositories;
using Media.Database.Repositories.Queries;
using Media.Database.Tests.TestHelpers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Npgsql;
using NUnit.Framework;
using Serilog.Core;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
#pragma warning disable CS8981
using pn = Media.Database.Repositories.Schemas.ParameterNames;
#pragma warning restore CS8981

namespace Media.Database.Tests.Repositories;

/// <summary>
/// Covers WordRepository's public API against a mocked ISqlQueryExecutor (and, for the
/// Scylla-first hydration path, a mocked ICqlQueryExecutor), mirroring FileRepositoryQueryTests.
/// </summary>
[TestFixture]
public class WordRepositoryQueryTests
{
    private Mock<ISqlQueryExecutor> _sqlExecutorMock = null!;
    private Mock<ICqlQueryExecutor> _cqlExecutorMock = null!;
    private Mock<Media.Common.Providers.IScyllaSessionProvider> _scyllaProviderMock = null!;
    private IFixture _fixture = null!;

    [SetUp]
    public void Setup()
    {
        _fixture = AutoMoqFixture.Create();
        _sqlExecutorMock = new Mock<ISqlQueryExecutor>();
        _cqlExecutorMock = new Mock<ICqlQueryExecutor>();
        _scyllaProviderMock = new Mock<Media.Common.Providers.IScyllaSessionProvider>();
        _scyllaProviderMock.Setup(p => p.MaxBatchSize).Returns(100);
    }

    private WordRepository CreateRepository() => new(
        _sqlExecutorMock.Object,
        _cqlExecutorMock.Object,
        _scyllaProviderMock.Object,
        Mock.Of<ILogger<WordRepository>>(),
        new LoggingLevelSwitch());

    [Test]
    public async Task GetFilePageIdentifiers_Should_UseWordOriginSql_ForWordOriginFileIdOrdering()
    {
        var expected = new List<WordFileIdentifier> { new() { WordId = 1, FileId = Guid.NewGuid(), Word = "w", OriginalFilePath = "p" } };
        _sqlExecutorMock
            .Setup(e => e.QueryManyAsync(QueryWords.GetFileIdentifiersByWordOriginSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, WordFileIdentifier>>()))
            .ReturnsAsync(expected);

        var result = await CreateRepository().GetFilePageIdentifiers(WordFilesOrderBy.WordOriginFileId, next: null, origin: null, isCurrent: null, isProperName: null, limit: 10);

        result.ShouldBe(expected);
    }

    [Test]
    public async Task GetFilePageIdentifiers_Should_UseFilePathOriginSql_ForFilePathOriginOrdering()
    {
        _sqlExecutorMock
            .Setup(e => e.QueryManyAsync(QueryWords.GetFileIdentifiersByFilePathOriginSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, WordFileIdentifier>>()))
            .ReturnsAsync([]);

        await CreateRepository().GetFilePageIdentifiers(WordFilesOrderBy.FilePathOrigin, next: null, origin: null, isCurrent: null, isProperName: null, limit: 10);

        _sqlExecutorMock.Verify(e => e.QueryManyAsync(QueryWords.GetFileIdentifiersByFilePathOriginSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, WordFileIdentifier>>()), Times.Once);
    }

    [Test]
    public async Task GetFilePageIdentifiers_Should_PassNextsFields_As_QueryParameters()
    {
        var next = new WordFileIdentifier { WordId = 1, FileId = Guid.NewGuid(), Word = "resume-word", OriginalFilePath = "resume-path" };
        Action<NpgsqlParameterCollection>? captured = null;
        _sqlExecutorMock
            .Setup(e => e.QueryManyAsync(QueryWords.GetFileIdentifiersByWordOriginSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, WordFileIdentifier>>()))
            .Callback<string, Action<NpgsqlParameterCollection>, Func<NpgsqlDataReader, WordFileIdentifier>>((_, configure, _) => captured = configure)
            .ReturnsAsync([]);

        await CreateRepository().GetFilePageIdentifiers(WordFilesOrderBy.WordOriginFileId, next, origin: null, isCurrent: null, isProperName: null, limit: 10);

        using var command = new NpgsqlCommand();
        captured!(command.Parameters);
        command.Parameters[pn.Word].Value.ShouldBe(next.Word);
        command.Parameters[pn.FileId].Value.ShouldBe(next.FileId);
        command.Parameters[pn.OriginalFilePath].Value.ShouldBe(next.OriginalFilePath);
    }

    [Test]
    public void GetFilePageIdentifiers_Should_PassNullValues_When_NextIsNull()
    {
        Action<NpgsqlParameterCollection>? captured = null;
        _sqlExecutorMock
            .Setup(e => e.QueryManyAsync(QueryWords.GetFileIdentifiersByWordOriginSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, WordFileIdentifier>>()))
            .Callback<string, Action<NpgsqlParameterCollection>, Func<NpgsqlDataReader, WordFileIdentifier>>((_, configure, _) => captured = configure)
            .ReturnsAsync([]);

        CreateRepository().GetFilePageIdentifiers(WordFilesOrderBy.WordOriginFileId, next: null, origin: null, isCurrent: null, isProperName: null, limit: 10).GetAwaiter().GetResult();

        using var command = new NpgsqlCommand();
        captured!(command.Parameters);
        command.Parameters[pn.Word].Value.ShouldBe(DBNull.Value);
        command.Parameters[pn.FileId].Value.ShouldBe(DBNull.Value);
        command.Parameters[pn.OriginalFilePath].Value.ShouldBe(DBNull.Value);
    }

    [Test]
    public async Task GetByIds_Should_PreferScylla_When_RowFound()
    {
        var wordId = 3;
        var fileId = Guid.NewGuid();
        var scyllaRow = new ViewWordFiles { WordId = wordId, FileId = fileId, Word = "w", OriginalFilePath = "p" };
        _cqlExecutorMock
            .Setup(e => e.QuerySingleAsync(QueryWords.GetWordFilesByWordIdAndFileIdCql, It.IsAny<Action<Dictionary<string, object>>>(), It.IsAny<Func<Cassandra.Row, ViewWordFiles>>()))
            .ReturnsAsync(scyllaRow);

        var result = await CreateRepository().GetByIds([(wordId, fileId)], maxDegreeOfParallelism: 1);

        result.ShouldBe([scyllaRow]);
        _sqlExecutorMock.Verify(e => e.QuerySingleAsync(QueryWords.GetViewByWordIdAndFileIdSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, ViewWordFiles>>()), Times.Never);
    }

    [Test]
    public async Task GetByIds_Should_FallBackToPostgres_When_ScyllaHasNoRowYet()
    {
        var wordId = 4;
        var fileId = Guid.NewGuid();
        var postgresRow = new ViewWordFiles { WordId = wordId, FileId = fileId, Word = "w", OriginalFilePath = "p" };
        _cqlExecutorMock
            .Setup(e => e.QuerySingleAsync(QueryWords.GetWordFilesByWordIdAndFileIdCql, It.IsAny<Action<Dictionary<string, object>>>(), It.IsAny<Func<Cassandra.Row, ViewWordFiles>>()))
            .ReturnsAsync((ViewWordFiles?)null);
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(QueryWords.GetViewByWordIdAndFileIdSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, ViewWordFiles>>()))
            .ReturnsAsync(postgresRow);

        var result = await CreateRepository().GetByIds([(wordId, fileId)], maxDegreeOfParallelism: 1);

        result.ShouldBe([postgresRow]);
    }

    [Test]
    public async Task GetByUuid_Should_ReturnWord_When_ExecutorFindsMatch()
    {
        var expected = _fixture.Create<Words>();
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(QueryWords.GetByUuidSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, Words>>()))
            .ReturnsAsync(expected);

        var result = await CreateRepository().GetByUuid(Guid.NewGuid());

        result.ShouldBe(expected);
    }

    [Test]
    public async Task GetByUuid_Should_ReturnNull_When_ExecutorFindsNoMatch()
    {
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(QueryWords.GetByUuidSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, Words>>()))
            .ReturnsAsync((Words?)null);

        var result = await CreateRepository().GetByUuid(Guid.NewGuid());

        result.ShouldBeNull();
    }

    [Test]
    public async Task Upsert_Should_Execute_UpsertWordSql()
    {
        var request = _fixture.Create<UpsertWordRequest>();

        await CreateRepository().Upsert(request);

        _sqlExecutorMock.Verify(e => e.ExecuteAsync(QueryWords.UpsertWordSql, It.IsAny<Action<NpgsqlParameterCollection>>()), Times.Once);
    }

    [Test]
    public void Upsert_Should_LogAndRethrow_When_ExecutorThrows()
    {
        _sqlExecutorMock
            .Setup(e => e.ExecuteAsync(QueryWords.UpsertWordSql, It.IsAny<Action<NpgsqlParameterCollection>>()))
            .ThrowsAsync(new InvalidOperationException("boom"));
        var request = _fixture.Create<UpsertWordRequest>();

        Should.ThrowAsync<InvalidOperationException>(() => CreateRepository().Upsert(request));
    }

    [Test]
    public async Task Upsert_Should_ThrowBadHttpRequestException_When_CameFromFileIdViolatesForeignKey()
    {
        var fkViolation = new PostgresException("insert or update on table \"WordFiles\" violates foreign key constraint \"FK_WordFiles_FileId\"", "ERROR", "ERROR", PostgresErrorCodes.ForeignKeyViolation);
        _sqlExecutorMock
            .Setup(e => e.ExecuteAsync(QueryWords.UpsertWordSql, It.IsAny<Action<NpgsqlParameterCollection>>()))
            .ThrowsAsync(fkViolation);
        var request = _fixture.Create<UpsertWordRequest>();

        var ex = await Should.ThrowAsync<BadHttpRequestException>(() => CreateRepository().Upsert(request));

        ex.Message.ShouldContain(request.CameFromFileId.ToString());
    }

    [Test]
    public async Task Upsert_Should_LogAndRethrow_PostgresException_When_Not_A_ForeignKeyViolation()
    {
        var uniqueViolation = new PostgresException("duplicate key value violates unique constraint", "ERROR", "ERROR", PostgresErrorCodes.UniqueViolation);
        _sqlExecutorMock
            .Setup(e => e.ExecuteAsync(QueryWords.UpsertWordSql, It.IsAny<Action<NpgsqlParameterCollection>>()))
            .ThrowsAsync(uniqueViolation);
        var request = _fixture.Create<UpsertWordRequest>();

        await Should.ThrowAsync<PostgresException>(() => CreateRepository().Upsert(request));
    }

    [Test]
    public async Task RefreshView_Should_Execute_RefreshViewSql()
    {
        await CreateRepository().RefreshView();

        _sqlExecutorMock.Verify(e => e.ExecuteAsync(QueryWords.RefreshViewSql, It.IsAny<Action<NpgsqlParameterCollection>>()), Times.Once);
    }

    [Test]
    public async Task DeleteByUuid_Should_Execute_DeleteByUuidSql()
    {
        await CreateRepository().DeleteByUuid(Guid.NewGuid());

        _sqlExecutorMock.Verify(e => e.ExecuteAsync(QueryWords.DeleteByUuidSql, It.IsAny<Action<NpgsqlParameterCollection>>()), Times.Once);
    }

    [Test]
    public async Task DeleteByUuid_Should_ConfigureUuidParameter()
    {
        Action<NpgsqlParameterCollection>? captured = null;
        _sqlExecutorMock
            .Setup(e => e.ExecuteAsync(QueryWords.DeleteByUuidSql, It.IsAny<Action<NpgsqlParameterCollection>>()))
            .Callback<string, Action<NpgsqlParameterCollection>>((_, configure) => captured = configure)
            .ReturnsAsync(1);
        var uuid = Guid.NewGuid();

        await CreateRepository().DeleteByUuid(uuid);

        using var command = new NpgsqlCommand();
        captured!(command.Parameters);
        command.Parameters[pn.Uuid].Value.ShouldBe(uuid);
    }

    [Test]
    public async Task GetWordsByFileId_Should_ReturnLinks_When_ExecutorFindsMatches()
    {
        var expected = new List<WordFileLink>
        {
            new(1, "alice", WordOrigin.Name),
            new(2, "paris", WordOrigin.FromLocation)
        };
        _sqlExecutorMock
            .Setup(e => e.QueryManyAsync(QueryWords.GetWordsByFileIdSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, WordFileLink>>()))
            .ReturnsAsync(expected);

        var result = await CreateRepository().GetWordsByFileId(Guid.NewGuid());

        result.ShouldBe(expected);
    }

    [Test]
    public async Task GetWordsByFileId_Should_ConfigureFileIdParameter()
    {
        Action<NpgsqlParameterCollection>? captured = null;
        _sqlExecutorMock
            .Setup(e => e.QueryManyAsync(QueryWords.GetWordsByFileIdSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, WordFileLink>>()))
            .Callback<string, Action<NpgsqlParameterCollection>, Func<NpgsqlDataReader, WordFileLink>>((_, configure, _) => captured = configure)
            .ReturnsAsync([]);
        var fileId = Guid.NewGuid();

        await CreateRepository().GetWordsByFileId(fileId);

        using var command = new NpgsqlCommand();
        captured!(command.Parameters);
        command.Parameters[pn.FileId].Value.ShouldBe(fileId);
    }

    [Test]
    public async Task DeleteWordFileLink_Should_ConfigureFileIdAndWordIdParameters()
    {
        Action<NpgsqlParameterCollection>? captured = null;
        _sqlExecutorMock
            .Setup(e => e.ExecuteAsync(QueryWords.DeleteWordFileLinkSql, It.IsAny<Action<NpgsqlParameterCollection>>()))
            .Callback<string, Action<NpgsqlParameterCollection>>((_, configure) => captured = configure);
        var fileId = Guid.NewGuid();

        await CreateRepository().DeleteWordFileLink(fileId, 42);

        using var command = new NpgsqlCommand();
        captured!(command.Parameters);
        command.Parameters[pn.FileId].Value.ShouldBe(fileId);
        command.Parameters[pn.WordId].Value.ShouldBe(42);

        _sqlExecutorMock.Verify(e => e.ExecuteAsync(QueryWords.DeleteWordFileLinkSql, It.IsAny<Action<NpgsqlParameterCollection>>()), Times.Once);
    }

    [Test]
    public async Task GetWordsByFileIdOrderedByWord_Should_ReturnResults_From_Executor()
    {
        var expected = _fixture.CreateMany<ViewWordFiles>(2).ToList();
        _sqlExecutorMock
            .Setup(e => e.QueryManyAsync(QueryWords.GetWordsByFileIdOrderedByWordSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, ViewWordFiles>>()))
            .ReturnsAsync(expected);

        var result = await CreateRepository().GetWordsByFileIdOrderedByWord(Guid.NewGuid(), 1, "after", true, false);

        result.ShouldBe(expected);
    }

    [Test]
    public async Task GetWordsByFileIdOrderedByWord_Should_ConfigureFileIdAndSourceMachineIdParameters()
    {
        Action<NpgsqlParameterCollection>? captured = null;
        _sqlExecutorMock
            .Setup(e => e.QueryManyAsync(QueryWords.GetWordsByFileIdOrderedByWordSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, ViewWordFiles>>()))
            .Callback<string, Action<NpgsqlParameterCollection>, Func<NpgsqlDataReader, ViewWordFiles>>((_, configure, _) => captured = configure)
            .ReturnsAsync([]);
        var fileId = Guid.NewGuid();

        await CreateRepository().GetWordsByFileIdOrderedByWord(fileId, 7, "after", true, false, 25);

        using var command = new NpgsqlCommand();
        captured!(command.Parameters);
        command.Parameters[pn.FileId].Value.ShouldBe(fileId);
        command.Parameters[pn.SourceMachineId].Value.ShouldBe(7);
        command.Parameters[pn.Word].Value.ShouldBe("after");
        command.Parameters[pn.IsCurrent].Value.ShouldBe(true);
        command.Parameters[pn.IsProperName].Value.ShouldBe(false);
        command.Parameters[pn.Limit].Value.ShouldBe(25);
    }

    [Test]
    public async Task GetWordsByFileIdOrderedByWord_Should_ConfigureNullOptionalParameters_As_DBNull()
    {
        Action<NpgsqlParameterCollection>? captured = null;
        _sqlExecutorMock
            .Setup(e => e.QueryManyAsync(QueryWords.GetWordsByFileIdOrderedByWordSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, ViewWordFiles>>()))
            .Callback<string, Action<NpgsqlParameterCollection>, Func<NpgsqlDataReader, ViewWordFiles>>((_, configure, _) => captured = configure)
            .ReturnsAsync([]);

        await CreateRepository().GetWordsByFileIdOrderedByWord(Guid.NewGuid(), 7, afterWord: null, isCurrent: null, isProperName: null);

        using var command = new NpgsqlCommand();
        captured!(command.Parameters);
        command.Parameters[pn.Word].Value.ShouldBe(DBNull.Value);
        command.Parameters[pn.IsCurrent].Value.ShouldBe(DBNull.Value);
        command.Parameters[pn.IsProperName].Value.ShouldBe(DBNull.Value);
    }

    [Test]
    public void GetWordsByFileIdOrderedByWord_Should_LogAndRethrow_When_ExecutorThrows()
    {
        _sqlExecutorMock
            .Setup(e => e.QueryManyAsync(QueryWords.GetWordsByFileIdOrderedByWordSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, ViewWordFiles>>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        Should.ThrowAsync<InvalidOperationException>(() => CreateRepository().GetWordsByFileIdOrderedByWord(Guid.NewGuid(), 1, "after", true, false));
    }

    [Test]
    public async Task GetWordsByFileIdOrderedByOrigin_Should_ReturnResults_From_Executor()
    {
        var expected = _fixture.CreateMany<ViewWordFiles>(2).ToList();
        _sqlExecutorMock
            .Setup(e => e.QueryManyAsync(QueryWords.GetWordsByFileIdOrderedByOriginSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, ViewWordFiles>>()))
            .ReturnsAsync(expected);

        var result = await CreateRepository().GetWordsByFileIdOrderedByOrigin(Guid.NewGuid(), 1, WordOrigin.Name, true, false);

        result.ShouldBe(expected);
    }

    [Test]
    public async Task GetWordsByFileIdOrderedByOrigin_Should_ConfigureFileIdAndSourceMachineIdParameters()
    {
        Action<NpgsqlParameterCollection>? captured = null;
        _sqlExecutorMock
            .Setup(e => e.QueryManyAsync(QueryWords.GetWordsByFileIdOrderedByOriginSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, ViewWordFiles>>()))
            .Callback<string, Action<NpgsqlParameterCollection>, Func<NpgsqlDataReader, ViewWordFiles>>((_, configure, _) => captured = configure)
            .ReturnsAsync([]);
        var fileId = Guid.NewGuid();

        await CreateRepository().GetWordsByFileIdOrderedByOrigin(fileId, 7, WordOrigin.Keyword, true, false, 25);

        using var command = new NpgsqlCommand();
        captured!(command.Parameters);
        command.Parameters[pn.FileId].Value.ShouldBe(fileId);
        command.Parameters[pn.SourceMachineId].Value.ShouldBe(7);
        command.Parameters[pn.Origin].Value.ShouldBe(WordOrigin.Keyword);
        command.Parameters[pn.IsCurrent].Value.ShouldBe(true);
        command.Parameters[pn.IsProperName].Value.ShouldBe(false);
        command.Parameters[pn.Limit].Value.ShouldBe(25);
    }

    [Test]
    public async Task GetWordsByFileIdOrderedByOrigin_Should_ConfigureNullOptionalParameters_As_DBNull()
    {
        Action<NpgsqlParameterCollection>? captured = null;
        _sqlExecutorMock
            .Setup(e => e.QueryManyAsync(QueryWords.GetWordsByFileIdOrderedByOriginSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, ViewWordFiles>>()))
            .Callback<string, Action<NpgsqlParameterCollection>, Func<NpgsqlDataReader, ViewWordFiles>>((_, configure, _) => captured = configure)
            .ReturnsAsync([]);

        await CreateRepository().GetWordsByFileIdOrderedByOrigin(Guid.NewGuid(), 7, afterOrigin: null, isCurrent: null, isProperName: null);

        using var command = new NpgsqlCommand();
        captured!(command.Parameters);
        command.Parameters[pn.Origin].Value.ShouldBe(DBNull.Value);
        command.Parameters[pn.IsCurrent].Value.ShouldBe(DBNull.Value);
        command.Parameters[pn.IsProperName].Value.ShouldBe(DBNull.Value);
    }

    [Test]
    public void GetWordsByFileIdOrderedByOrigin_Should_LogAndRethrow_When_ExecutorThrows()
    {
        _sqlExecutorMock
            .Setup(e => e.QueryManyAsync(QueryWords.GetWordsByFileIdOrderedByOriginSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, ViewWordFiles>>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        Should.ThrowAsync<InvalidOperationException>(() => CreateRepository().GetWordsByFileIdOrderedByOrigin(Guid.NewGuid(), 1, WordOrigin.Name, true, false));
    }

    [Test]
    public async Task GetWordsByFilePathOrderedByWord_Should_ReturnResults_From_Executor()
    {
        var expected = _fixture.CreateMany<ViewWordFiles>(2).ToList();
        _sqlExecutorMock
            .Setup(e => e.QueryManyAsync(QueryWords.GetWordsByFilePathOrderedByWordSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, ViewWordFiles>>()))
            .ReturnsAsync(expected);

        var result = await CreateRepository().GetWordsByFilePathOrderedByWord("/path", 1, "after", true, false);

        result.ShouldBe(expected);
    }

    [Test]
    public async Task GetWordsByFilePathOrderedByWord_Should_ConfigureFilePathAndSourceMachineIdParameters()
    {
        Action<NpgsqlParameterCollection>? captured = null;
        _sqlExecutorMock
            .Setup(e => e.QueryManyAsync(QueryWords.GetWordsByFilePathOrderedByWordSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, ViewWordFiles>>()))
            .Callback<string, Action<NpgsqlParameterCollection>, Func<NpgsqlDataReader, ViewWordFiles>>((_, configure, _) => captured = configure)
            .ReturnsAsync([]);

        await CreateRepository().GetWordsByFilePathOrderedByWord("/path/to/file.txt", 7, "after", true, false, 25);

        using var command = new NpgsqlCommand();
        captured!(command.Parameters);
        command.Parameters[pn.OriginalFilePath].Value.ShouldBe("/path/to/file.txt");
        command.Parameters[pn.SourceMachineId].Value.ShouldBe(7);
        command.Parameters[pn.Word].Value.ShouldBe("after");
        command.Parameters[pn.IsCurrent].Value.ShouldBe(true);
        command.Parameters[pn.IsProperName].Value.ShouldBe(false);
        command.Parameters[pn.Limit].Value.ShouldBe(25);
    }

    [Test]
    public async Task GetWordsByFilePathOrderedByWord_Should_ConfigureNullOptionalParameters_As_DBNull()
    {
        Action<NpgsqlParameterCollection>? captured = null;
        _sqlExecutorMock
            .Setup(e => e.QueryManyAsync(QueryWords.GetWordsByFilePathOrderedByWordSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, ViewWordFiles>>()))
            .Callback<string, Action<NpgsqlParameterCollection>, Func<NpgsqlDataReader, ViewWordFiles>>((_, configure, _) => captured = configure)
            .ReturnsAsync([]);

        await CreateRepository().GetWordsByFilePathOrderedByWord("/path", 7, afterWord: null, isCurrent: null, isProperName: null);

        using var command = new NpgsqlCommand();
        captured!(command.Parameters);
        command.Parameters[pn.Word].Value.ShouldBe(DBNull.Value);
        command.Parameters[pn.IsCurrent].Value.ShouldBe(DBNull.Value);
        command.Parameters[pn.IsProperName].Value.ShouldBe(DBNull.Value);
    }

    [Test]
    public void GetWordsByFilePathOrderedByWord_Should_LogAndRethrow_When_ExecutorThrows()
    {
        _sqlExecutorMock
            .Setup(e => e.QueryManyAsync(QueryWords.GetWordsByFilePathOrderedByWordSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, ViewWordFiles>>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        Should.ThrowAsync<InvalidOperationException>(() => CreateRepository().GetWordsByFilePathOrderedByWord("/path", 1, "after", true, false));
    }
}
