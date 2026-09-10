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
/// Covers WordRepository's public API against a mocked ISqlQueryExecutor, mirroring
/// FileRepositoryQueryTests. WordRepository never touches Scylla.
/// </summary>
[TestFixture]
public class WordRepositoryQueryTests
{
    private Mock<ISqlQueryExecutor> _sqlExecutorMock = null!;
    private IFixture _fixture = null!;

    [SetUp]
    public void Setup()
    {
        _fixture = AutoMoqFixture.Create();
        _sqlExecutorMock = new Mock<ISqlQueryExecutor>();
    }

    private WordRepository CreateRepository() => new(
        _sqlExecutorMock.Object,
        Mock.Of<ILogger<WordRepository>>(),
        new LoggingLevelSwitch());

    [Test]
    public async Task GetById_Should_ReturnWord_When_ExecutorFindsMatch()
    {
        var expected = _fixture.Create<Words>();
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(QueryWords.GetByIdSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, Words>>()))
            .ReturnsAsync(expected);

        var result = await CreateRepository().GetById(5);

        result.ShouldBe(expected);
    }

    [Test]
    public async Task GetById_Should_ReturnNull_When_ExecutorFindsNoMatch()
    {
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(QueryWords.GetByIdSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, Words>>()))
            .ReturnsAsync((Words?)null);

        var result = await CreateRepository().GetById(5);

        result.ShouldBeNull();
    }

    [Test]
    public async Task GetFilePagesByWordOrigin_Should_ReturnResults_From_Executor()
    {
        var expected = _fixture.CreateMany<ViewWordFiles>(2).ToList();
        _sqlExecutorMock
            .Setup(e => e.QueryManyAsync(QueryWords.GetFilePagesByWordOriginSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, ViewWordFiles>>()))
            .ReturnsAsync(expected);

        var result = await CreateRepository().GetFilePagesByWordOrigin("word", WordOrigin.Name, Guid.NewGuid(), true, false);

        result.ShouldBe(expected);
    }

    [Test]
    public async Task GetFilePages_Should_ConfigureNonNullParameters_When_AllArgumentsProvided()
    {
        Action<NpgsqlParameterCollection>? captured = null;
        _sqlExecutorMock
            .Setup(e => e.QueryManyAsync(QueryWords.GetFilePagesByWordFileIdSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, ViewWordFiles>>()))
            .Callback<string, Action<NpgsqlParameterCollection>, Func<NpgsqlDataReader, ViewWordFiles>>((_, configure, _) => captured = configure)
            .ReturnsAsync([]);
        var fileId = Guid.NewGuid();

        await CreateRepository().GetFilePages(QueryWords.GetFilePagesByWordFileIdSql, "word", WordOrigin.Name, fileId, true, false);

        using var command = new NpgsqlCommand();
        captured!(command.Parameters);
        command.Parameters[pn.Word].Value.ShouldBe("word");
        command.Parameters[pn.Origin].Value.ShouldBe(WordOrigin.Name);
        command.Parameters[pn.FileId].Value.ShouldBe(fileId);
        command.Parameters[pn.IsCurrent].Value.ShouldBe(true);
        command.Parameters[pn.IsProperName].Value.ShouldBe(false);
    }

    [Test]
    public async Task GetFilePages_Should_ConfigureDbNullParameters_When_AllArgumentsNull()
    {
        Action<NpgsqlParameterCollection>? captured = null;
        _sqlExecutorMock
            .Setup(e => e.QueryManyAsync(QueryWords.GetFilePagesByWordFileIdSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, ViewWordFiles>>()))
            .Callback<string, Action<NpgsqlParameterCollection>, Func<NpgsqlDataReader, ViewWordFiles>>((_, configure, _) => captured = configure)
            .ReturnsAsync([]);

        await CreateRepository().GetFilePages(QueryWords.GetFilePagesByWordFileIdSql, null, null, null, null, null);

        using var command = new NpgsqlCommand();
        captured!(command.Parameters);
        command.Parameters[pn.Word].Value.ShouldBe(DBNull.Value);
        command.Parameters[pn.Origin].Value.ShouldBe(DBNull.Value);
        command.Parameters[pn.FileId].Value.ShouldBe(DBNull.Value);
        command.Parameters[pn.IsCurrent].Value.ShouldBe(DBNull.Value);
        command.Parameters[pn.IsProperName].Value.ShouldBe(DBNull.Value);
    }

    /// <summary>
    /// Each "By*" variant must query its own like-named sort-order query — not a shared one.
    /// Regression coverage for a wiring bug where three of the four variants all silently
    /// executed <see cref="QueryWords.GetFilePagesByWordFileIdSql"/> regardless of which
    /// sort order their name promised.
    /// </summary>
    [TestCase(nameof(WordRepository.GetFilePagesByWordOrigin))]
    [TestCase(nameof(WordRepository.GetFilePagesByWordFileId))]
    [TestCase(nameof(WordRepository.GetFilePagesByFileIdOrigin))]
    [TestCase(nameof(WordRepository.GetFilePagesByFileIdWord))]
    public async Task GetFilePagesBy_Variants_Should_Query_TheirOwnLikeNamedSql(string methodName)
    {
        var expectedSql = methodName switch
        {
            nameof(WordRepository.GetFilePagesByWordOrigin) => QueryWords.GetFilePagesByWordOriginSql,
            nameof(WordRepository.GetFilePagesByWordFileId) => QueryWords.GetFilePagesByWordFileIdSql,
            nameof(WordRepository.GetFilePagesByFileIdOrigin) => QueryWords.GetFilePagesByFileIdOriginSql,
            nameof(WordRepository.GetFilePagesByFileIdWord) => QueryWords.GetFilePagesByFileIdWordSql,
            _ => throw new ArgumentOutOfRangeException(nameof(methodName))
        };
        var expected = _fixture.CreateMany<ViewWordFiles>(2).ToList();
        _sqlExecutorMock
            .Setup(e => e.QueryManyAsync(expectedSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, ViewWordFiles>>()))
            .ReturnsAsync(expected);
        var repository = CreateRepository();
        var method = typeof(WordRepository).GetMethod(methodName)!;

        var task = (Task<List<ViewWordFiles>>)method.Invoke(repository, [null, null, null, null, null, 10])!;
        var result = await task;

        result.ShouldBe(expected);
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
    public async Task Delete_Should_Execute_DeleteWordSql()
    {
        await CreateRepository().Delete(5);

        _sqlExecutorMock.Verify(e => e.ExecuteAsync(QueryWords.DeleteWordSql, It.IsAny<Action<NpgsqlParameterCollection>>()), Times.Once);
    }

    [Test]
    public async Task Delete_Should_ConfigureIdParameter()
    {
        Action<NpgsqlParameterCollection>? captured = null;
        _sqlExecutorMock
            .Setup(e => e.ExecuteAsync(QueryWords.DeleteWordSql, It.IsAny<Action<NpgsqlParameterCollection>>()))
            .Callback<string, Action<NpgsqlParameterCollection>>((_, configure) => captured = configure)
            .ReturnsAsync(1);

        await CreateRepository().Delete(5);

        using var command = new NpgsqlCommand();
        captured!(command.Parameters);
        command.Parameters[pn.Id].Value.ShouldBe(5);
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
