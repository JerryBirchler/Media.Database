#nullable enable
using Media.Common.Cdc;
using Media.Common.Providers;
using Media.Database.Repositories;
using Media.Database.Repositories.Cdc;
using Media.Database.Repositories.Queries;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Media.Database.Tests.Repositories.Cdc;

[TestFixture]
public class FilesCdcSyncHandlerTests
{
    private Mock<ICqlQueryExecutor> _cqlExecutorMock = null!;
    private Mock<IScyllaSessionProvider> _scyllaProviderMock = null!;

    [SetUp]
    public void Setup()
    {
        _cqlExecutorMock = new Mock<ICqlQueryExecutor>();
        _scyllaProviderMock = new Mock<IScyllaSessionProvider>();
        _scyllaProviderMock.Setup(p => p.MaxBatchSize).Returns(100);
    }

    private FilesCdcSyncHandler CreateHandler() => new(
        _cqlExecutorMock.Object,
        _scyllaProviderMock.Object,
        Mock.Of<ILogger<FilesCdcSyncHandler>>());

    private static CdcChangeRecord UpsertRecord(Guid id, bool isCurrent = true, string? metadataJson = "{\"title\":\"t\"}")
    {
        var payload = new
        {
            Id = id,
            SourceMachineId = 1,
            OriginalFilePath = "c:\temp\a.png",
            InsertedOn = "2026-07-25T01:35:45.110Z",
            UpdatedOn = "2026-08-27T18:28:07.082Z",
            IsCurrent = isCurrent,
            Metadata = metadataJson,
            LastFileUpdate = "2026-07-27T17:43:00.000Z",
            __source_ts_ms = 1788574857777L,
            __op = "u"
        };
        var after = JsonDocument.Parse(JsonSerializer.Serialize(payload)).RootElement;
        return new CdcChangeRecord("cdc.public.Files", BuildKey(id), after, IsDeleted: false, SourceTimestampMs: 1788574857777, Offset: 0);
    }

    private static CdcChangeRecord DeleteRecord(Guid id) =>
        new("cdc.public.Files", BuildKey(id), After: null, IsDeleted: true, SourceTimestampMs: 0, Offset: 1);

    private static string BuildKey(Guid id) => JsonSerializer.Serialize(new { Id = id });

    [Test]
    public async Task ApplyAsync_Should_Upsert_When_Record_Is_Not_Deleted()
    {
        var id = Guid.NewGuid();
        var sut = CreateHandler();

        await sut.ApplyAsync(UpsertRecord(id), CancellationToken.None);

        _cqlExecutorMock.Verify(e => e.ExecuteAsync(QueryFiles.UpsertCql, It.IsAny<Action<SortedDictionary<string, object>>>()), Times.Once);
        _cqlExecutorMock.Verify(e => e.ExecuteAsync(QueryFiles.DeleteCql, It.IsAny<Action<SortedDictionary<string, object>>>()), Times.Never);
    }

    [Test]
    public async Task ApplyAsync_Should_Pass_Correct_Field_Values_To_UpsertCql()
    {
        var id = Guid.NewGuid();
        var sut = CreateHandler();
        SortedDictionary<string, object>? captured = null;
        _cqlExecutorMock
            .Setup(e => e.ExecuteAsync(QueryFiles.UpsertCql, It.IsAny<Action<SortedDictionary<string, object>>>()))
            .Callback<string, Action<SortedDictionary<string, object>>>((_, configure) =>
            {
                captured = new SortedDictionary<string, object>();
                configure(captured);
            })
            .Returns(Task.CompletedTask);

        await sut.ApplyAsync(UpsertRecord(id, isCurrent: false), CancellationToken.None);

        captured.ShouldNotBeNull();
        captured!["@ID"].ShouldBe(id);
        captured["@SOURCEMACHINEID"].ShouldBe(1);
        captured["@ORIGINALFILEPATH"].ShouldBe("c:\temp\a.png");
        captured["@ISCURRENT"].ShouldBe(false);
    }

    [Test]
    public async Task ApplyAsync_Should_Pass_Null_Metadata_When_Metadata_Is_Json_Null()
    {
        var id = Guid.NewGuid();
        var sut = CreateHandler();
        SortedDictionary<string, object>? captured = null;
        _cqlExecutorMock
            .Setup(e => e.ExecuteAsync(QueryFiles.UpsertCql, It.IsAny<Action<SortedDictionary<string, object>>>()))
            .Callback<string, Action<SortedDictionary<string, object>>>((_, configure) =>
            {
                captured = new SortedDictionary<string, object>();
                configure(captured);
            })
            .Returns(Task.CompletedTask);

        await sut.ApplyAsync(UpsertRecord(id, metadataJson: null), CancellationToken.None);

        captured.ShouldNotBeNull();
        captured!["@METADATA"].ShouldBeNull();
    }

    [Test]
    public async Task ApplyAsync_Should_Delete_When_Record_Is_Deleted()
    {
        var id = Guid.NewGuid();
        var sut = CreateHandler();
        SortedDictionary<string, object>? captured = null;
        _cqlExecutorMock
            .Setup(e => e.ExecuteAsync(QueryFiles.DeleteCql, It.IsAny<Action<SortedDictionary<string, object>>>()))
            .Callback<string, Action<SortedDictionary<string, object>>>((_, configure) =>
            {
                captured = new SortedDictionary<string, object>();
                configure(captured);
            })
            .Returns(Task.CompletedTask);

        await sut.ApplyAsync(DeleteRecord(id), CancellationToken.None);

        _cqlExecutorMock.Verify(e => e.ExecuteAsync(QueryFiles.UpsertCql, It.IsAny<Action<SortedDictionary<string, object>>>()), Times.Never);
        captured.ShouldNotBeNull();
        captured!["@ID"].ShouldBe(id);
    }

    [Test]
    public void Topics_Should_Contain_Only_CdcPublicFiles()
    {
        var sut = CreateHandler();

        sut.Topics.ShouldBe(["cdc.public.Files"]);
    }
}
