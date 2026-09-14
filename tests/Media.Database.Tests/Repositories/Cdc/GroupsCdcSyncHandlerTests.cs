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
public class GroupsCdcSyncHandlerTests
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

    private GroupsCdcSyncHandler CreateHandler() => new(
        _cqlExecutorMock.Object,
        _scyllaProviderMock.Object,
        Mock.Of<ILogger<GroupsCdcSyncHandler>>());

    private static CdcChangeRecord UpsertRecord(int groupId, string? description = "a description", bool isActive = true)
    {
        var payload = new
        {
            GroupId = groupId,
            GroupUuid = Guid.NewGuid(),
            Name = "group-name",
            Title = "Group Title",
            Description = description,
            IsActive = isActive,
            InsertedOn = "2026-07-25T01:35:45.110Z",
            UpdatedOn = "2026-08-27T18:28:07.082Z",
            __source_ts_ms = 1788574857777L,
            __op = "u"
        };
        var after = JsonDocument.Parse(JsonSerializer.Serialize(payload)).RootElement;
        return new CdcChangeRecord("cdc.public.Groups", BuildKey(groupId), after, IsDeleted: false, SourceTimestampMs: 1788574857777, Offset: 0);
    }

    private static CdcChangeRecord DeleteRecord(int groupId) =>
        new("cdc.public.Groups", BuildKey(groupId), After: null, IsDeleted: true, SourceTimestampMs: 0, Offset: 1);

    private static string BuildKey(int groupId) => JsonSerializer.Serialize(new { GroupId = groupId });

    [Test]
    public async Task ApplyAsync_Should_Upsert_When_Record_Is_Not_Deleted()
    {
        var sut = CreateHandler();

        await sut.ApplyAsync(UpsertRecord(1), CancellationToken.None);

        _cqlExecutorMock.Verify(e => e.ExecuteAsync(QueryGroups.UpsertCql, It.IsAny<Action<Dictionary<string, object>>>()), Times.Once);
        _cqlExecutorMock.Verify(e => e.ExecuteAsync(QueryGroups.DeleteCql, It.IsAny<Action<Dictionary<string, object>>>()), Times.Never);
    }

    [Test]
    public async Task ApplyAsync_Should_Pass_Correct_Field_Values_To_UpsertCql()
    {
        var sut = CreateHandler();
        Dictionary<string, object>? captured = null;
        _cqlExecutorMock
            .Setup(e => e.ExecuteAsync(QueryGroups.UpsertCql, It.IsAny<Action<Dictionary<string, object>>>()))
            .Callback<string, Action<Dictionary<string, object>>>((_, configure) =>
            {
                captured = new Dictionary<string, object>();
                configure(captured);
            })
            .Returns(Task.CompletedTask);

        await sut.ApplyAsync(UpsertRecord(7, isActive: false), CancellationToken.None);

        captured.ShouldNotBeNull();
        captured!["@GROUPID"].ShouldBe(7);
        captured["@NAME"].ShouldBe("group-name");
        captured["@TITLE"].ShouldBe("Group Title");
        captured["@ISACTIVE"].ShouldBe(false);
    }

    [Test]
    public async Task ApplyAsync_Should_Pass_Null_Description_When_Description_Is_Json_Null()
    {
        var sut = CreateHandler();
        Dictionary<string, object>? captured = null;
        _cqlExecutorMock
            .Setup(e => e.ExecuteAsync(QueryGroups.UpsertCql, It.IsAny<Action<Dictionary<string, object>>>()))
            .Callback<string, Action<Dictionary<string, object>>>((_, configure) =>
            {
                captured = new Dictionary<string, object>();
                configure(captured);
            })
            .Returns(Task.CompletedTask);

        await sut.ApplyAsync(UpsertRecord(1, description: null), CancellationToken.None);

        captured.ShouldNotBeNull();
        captured!["@DESCRIPTION"].ShouldBeNull();
    }

    [Test]
    public async Task ApplyAsync_Should_Delete_When_Record_Is_Deleted()
    {
        var sut = CreateHandler();
        Dictionary<string, object>? captured = null;
        _cqlExecutorMock
            .Setup(e => e.ExecuteAsync(QueryGroups.DeleteCql, It.IsAny<Action<Dictionary<string, object>>>()))
            .Callback<string, Action<Dictionary<string, object>>>((_, configure) =>
            {
                captured = new Dictionary<string, object>();
                configure(captured);
            })
            .Returns(Task.CompletedTask);

        await sut.ApplyAsync(DeleteRecord(3), CancellationToken.None);

        _cqlExecutorMock.Verify(e => e.ExecuteAsync(QueryGroups.UpsertCql, It.IsAny<Action<Dictionary<string, object>>>()), Times.Never);
        captured.ShouldNotBeNull();
        captured!["@GROUPID"].ShouldBe(3);
    }

    [Test]
    public void Topics_Should_Contain_Only_CdcPublicGroups()
    {
        var sut = CreateHandler();

        sut.Topics.ShouldBe(["cdc.public.Groups"]);
    }
}
