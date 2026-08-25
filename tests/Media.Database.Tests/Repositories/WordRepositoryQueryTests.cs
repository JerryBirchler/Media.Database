#nullable enable
using AutoFixture;
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
using System.Threading.Tasks;

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
            .Setup(e => e.QueryManyAsync(QueryWords.GetFilePagesByWordFileIdSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, ViewWordFiles>>()))
            .ReturnsAsync(expected);

        var result = await CreateRepository().GetFilePagesByWordOrigin("word", WordOrigin.Name, Guid.NewGuid(), true, false);

        result.ShouldBe(expected);
    }

    [TestCase(nameof(WordRepository.GetFilePagesByWordFileId))]
    [TestCase(nameof(WordRepository.GetFilePagesByFileIdOrigin))]
    [TestCase(nameof(WordRepository.GetFilePagesByFileIdWord))]
    public async Task GetFilePagesBy_Variants_Should_All_Query_GetFilePagesByWordFileIdSql(string methodName)
    {
        var expected = _fixture.CreateMany<ViewWordFiles>(2).ToList();
        _sqlExecutorMock
            .Setup(e => e.QueryManyAsync(QueryWords.GetFilePagesByWordFileIdSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, ViewWordFiles>>()))
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
    public async Task RefreshView_Should_Execute_RefreshViewSql()
    {
        await CreateRepository().RefreshView();

        _sqlExecutorMock.Verify(e => e.ExecuteAsync(QueryWords.RefreshViewSql, It.IsAny<Action<NpgsqlParameterCollection>>()), Times.Once);
    }

    [Test]
    public async Task Delete_Should_Execute_DeleteSql()
    {
        await CreateRepository().Delete(5);

        _sqlExecutorMock.Verify(e => e.ExecuteAsync(QueryFiles.DeleteSql, It.IsAny<Action<NpgsqlParameterCollection>>()), Times.Once);
    }

    [Test]
    public async Task DeleteFile_Should_Execute_DeleteFileSql()
    {
        await CreateRepository().DeleteFile(Guid.NewGuid());

        _sqlExecutorMock.Verify(e => e.ExecuteAsync(QueryWords.DeleteFileSql, It.IsAny<Action<NpgsqlParameterCollection>>()), Times.Once);
    }
}
