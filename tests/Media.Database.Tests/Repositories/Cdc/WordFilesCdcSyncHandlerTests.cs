#nullable enable
using Media.Common.Cdc;
using Media.Common.Providers;
using Media.Database.Models;
using Media.Database.Repositories;
using Media.Database.Repositories.Cdc;
using Media.Database.Repositories.Queries;
using Microsoft.Extensions.Logging;
using Moq;
using Npgsql;
using NUnit.Framework;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Media.Database.Tests.Repositories.Cdc;

[TestFixture]
public class WordFilesCdcSyncHandlerTests
{
    private Mock<ISqlQueryExecutor> _sqlExecutorMock = null!;
    private Mock<ICqlQueryExecutor> _cqlExecutorMock = null!;
    private Mock<IScyllaSessionProvider> _scyllaProviderMock = null!;

    [SetUp]
    public void Setup()
    {
        _sqlExecutorMock = new Mock<ISqlQueryExecutor>();
        _cqlExecutorMock = new Mock<ICqlQueryExecutor>();
        _scyllaProviderMock = new Mock<IScyllaSessionProvider>();
        _scyllaProviderMock.Setup(p => p.MaxBatchSize).Returns(100);
    }

    private WordFilesCdcSyncHandler CreateHandler() => new(
        _sqlExecutorMock.Object,
        _cqlExecutorMock.Object,
        _scyllaProviderMock.Object,
        Mock.Of<ILogger<WordFilesCdcSyncHandler>>());

    private static ViewWordFiles SampleRow(int wordId, Guid fileId) => new()
    {
        WordId = wordId,
        FileId = fileId,
        Origin = WordOrigin.Keyword,
        Word = "word",
        IsCurrent = true,
        IsProperName = false,
        OriginalFilePath = "path.txt",
        SourceMachineId = 1,
        ThumbnailGeneratedOn = null
    };

    [Test]
    public void Topics_Should_Contain_AllThreeSourceTopics()
    {
        CreateHandler().Topics.ShouldBe(["cdc.public.WordFiles", "cdc.public.Words", "cdc.public.Files"]);
    }

    [Test]
    public async Task ApplyAsync_Should_Upsert_When_WordFilesRecord_Found_On_Requery()
    {
        var wordId = 5;
        var fileId = Guid.NewGuid();
        var after = JsonDocument.Parse(JsonSerializer.Serialize(new { WordId = wordId, FileId = fileId })).RootElement;
        var record = new CdcChangeRecord("cdc.public.WordFiles", "key", after, IsDeleted: false, SourceTimestampMs: 0, Offset: 0);
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(QueryWords.GetViewByWordIdAndFileIdSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, ViewWordFiles>>()))
            .ReturnsAsync(SampleRow(wordId, fileId));

        await CreateHandler().ApplyAsync(record, CancellationToken.None);

        _cqlExecutorMock.Verify(e => e.ExecuteAsync(QueryWords.UpsertWordFilesCql, It.IsAny<Action<Dictionary<string, object>>>()), Times.Once);
    }

    [Test]
    public async Task ApplyAsync_Should_NotUpsert_When_WordFilesRequery_FindsNothing()
    {
        var after = JsonDocument.Parse(JsonSerializer.Serialize(new { WordId = 5, FileId = Guid.NewGuid() })).RootElement;
        var record = new CdcChangeRecord("cdc.public.WordFiles", "key", after, IsDeleted: false, SourceTimestampMs: 0, Offset: 0);
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(QueryWords.GetViewByWordIdAndFileIdSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, ViewWordFiles>>()))
            .ReturnsAsync((ViewWordFiles?)null);

        await CreateHandler().ApplyAsync(record, CancellationToken.None);

        _cqlExecutorMock.Verify(e => e.ExecuteAsync(QueryWords.UpsertWordFilesCql, It.IsAny<Action<Dictionary<string, object>>>()), Times.Never);
    }

    [Test]
    public async Task ApplyAsync_Should_Delete_When_WordFilesRecord_IsDeleted()
    {
        var wordId = 7;
        var fileId = Guid.NewGuid();
        var key = JsonSerializer.Serialize(new { WordId = wordId, FileId = fileId });
        var record = new CdcChangeRecord("cdc.public.WordFiles", key, After: null, IsDeleted: true, SourceTimestampMs: 0, Offset: 0);
        Dictionary<string, object>? captured = null;
        _cqlExecutorMock
            .Setup(e => e.ExecuteAsync(QueryWords.DeleteWordFilesCql, It.IsAny<Action<Dictionary<string, object>>>()))
            .Callback<string, Action<Dictionary<string, object>>>((_, configure) =>
            {
                captured = [];
                configure(captured);
            })
            .Returns(Task.CompletedTask);

        await CreateHandler().ApplyAsync(record, CancellationToken.None);

        captured.ShouldNotBeNull();
        captured!["@WORDID"].ShouldBe(wordId);
        captured["@FILEID"].ShouldBe(fileId);
        _cqlExecutorMock.Verify(e => e.ExecuteAsync(QueryWords.UpsertWordFilesCql, It.IsAny<Action<Dictionary<string, object>>>()), Times.Never);
    }

    [Test]
    public async Task ApplyAsync_Should_UpsertEveryLinkedFile_When_WordsRecord_Changed()
    {
        var wordId = 9;
        var rows = new List<ViewWordFiles> { SampleRow(wordId, Guid.NewGuid()), SampleRow(wordId, Guid.NewGuid()) };
        var after = JsonDocument.Parse(JsonSerializer.Serialize(new { Id = wordId })).RootElement;
        var record = new CdcChangeRecord("cdc.public.Words", "key", after, IsDeleted: false, SourceTimestampMs: 0, Offset: 0);
        _sqlExecutorMock
            .Setup(e => e.QueryManyAsync(QueryWords.GetViewByWordIdSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, ViewWordFiles>>()))
            .ReturnsAsync(rows);

        await CreateHandler().ApplyAsync(record, CancellationToken.None);

        _cqlExecutorMock.Verify(e => e.ExecuteAsync(QueryWords.UpsertWordFilesCql, It.IsAny<Action<Dictionary<string, object>>>()), Times.Exactly(2));
    }

    [Test]
    public async Task ApplyAsync_Should_DoNothing_When_WordsRecord_IsDeleted()
    {
        var record = new CdcChangeRecord("cdc.public.Words", "key", After: null, IsDeleted: true, SourceTimestampMs: 0, Offset: 0);

        await CreateHandler().ApplyAsync(record, CancellationToken.None);

        _sqlExecutorMock.Verify(e => e.QueryManyAsync(QueryWords.GetViewByWordIdSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, ViewWordFiles>>()), Times.Never);
    }

    [Test]
    public async Task ApplyAsync_Should_UpsertEveryLinkedWord_When_FilesRecord_Changed()
    {
        var fileId = Guid.NewGuid();
        var rows = new List<ViewWordFiles> { SampleRow(1, fileId), SampleRow(2, fileId), SampleRow(3, fileId) };
        var after = JsonDocument.Parse(JsonSerializer.Serialize(new { Id = fileId })).RootElement;
        var record = new CdcChangeRecord("cdc.public.Files", "key", after, IsDeleted: false, SourceTimestampMs: 0, Offset: 0);
        _sqlExecutorMock
            .Setup(e => e.QueryManyAsync(QueryWords.GetViewByFileIdSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, ViewWordFiles>>()))
            .ReturnsAsync(rows);

        await CreateHandler().ApplyAsync(record, CancellationToken.None);

        _cqlExecutorMock.Verify(e => e.ExecuteAsync(QueryWords.UpsertWordFilesCql, It.IsAny<Action<Dictionary<string, object>>>()), Times.Exactly(3));
    }

    [Test]
    public async Task ApplyAsync_Should_DoNothing_When_FilesRecord_IsDeleted()
    {
        var record = new CdcChangeRecord("cdc.public.Files", "key", After: null, IsDeleted: true, SourceTimestampMs: 0, Offset: 0);

        await CreateHandler().ApplyAsync(record, CancellationToken.None);

        _sqlExecutorMock.Verify(e => e.QueryManyAsync(QueryWords.GetViewByFileIdSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, ViewWordFiles>>()), Times.Never);
    }

    [Test]
    public async Task ApplyAsync_Should_PassCorrectFieldValues_To_UpsertWordFilesCql()
    {
        var wordId = 11;
        var fileId = Guid.NewGuid();
        var generatedOn = DateTimeOffset.UtcNow;
        var row = SampleRow(wordId, fileId);
        row.ThumbnailGeneratedOn = generatedOn;
        var after = JsonDocument.Parse(JsonSerializer.Serialize(new { WordId = wordId, FileId = fileId })).RootElement;
        var record = new CdcChangeRecord("cdc.public.WordFiles", "key", after, IsDeleted: false, SourceTimestampMs: 0, Offset: 0);
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(QueryWords.GetViewByWordIdAndFileIdSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, ViewWordFiles>>()))
            .ReturnsAsync(row);
        Dictionary<string, object>? captured = null;
        _cqlExecutorMock
            .Setup(e => e.ExecuteAsync(QueryWords.UpsertWordFilesCql, It.IsAny<Action<Dictionary<string, object>>>()))
            .Callback<string, Action<Dictionary<string, object>>>((_, configure) =>
            {
                captured = [];
                configure(captured);
            })
            .Returns(Task.CompletedTask);

        await CreateHandler().ApplyAsync(record, CancellationToken.None);

        captured.ShouldNotBeNull();
        captured!["@WORDID"].ShouldBe(wordId);
        captured["@FILEID"].ShouldBe(fileId);
        captured["@THUMBNAILGENERATEDON"].ShouldBe(generatedOn);
    }
}
