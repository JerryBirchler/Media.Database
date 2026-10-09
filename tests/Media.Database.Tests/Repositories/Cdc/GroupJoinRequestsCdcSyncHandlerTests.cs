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
public class GroupJoinRequestsCdcSyncHandlerTests
{
    private Mock<ICqlQueryExecutor> _cqlExecutorMock = null!;
    private Mock<IScyllaSessionProvider> _scyllaProviderMock = null!;
    private Dictionary<string, object>? _captured;

    [SetUp]
    public void Setup()
    {
        _cqlExecutorMock = new Mock<ICqlQueryExecutor>();
        _scyllaProviderMock = new Mock<IScyllaSessionProvider>();
        _scyllaProviderMock.Setup(p => p.MaxBatchSize).Returns(100);
        _captured = null;
        _cqlExecutorMock
            .Setup(e => e.ExecuteAsync(It.IsAny<string>(), It.IsAny<Action<Dictionary<string, object>>>()))
            .Callback<string, Action<Dictionary<string, object>>>((_, configure) =>
            {
                _captured = new Dictionary<string, object>();
                configure(_captured);
            })
            .Returns(Task.CompletedTask);
    }

    private GroupJoinRequestsCdcSyncHandler CreateHandler() => new(
        _cqlExecutorMock.Object,
        _scyllaProviderMock.Object,
        Mock.Of<ILogger<GroupJoinRequestsCdcSyncHandler>>());

    private static string BuildKey(int id) => JsonSerializer.Serialize(new { GroupJoinRequestId = id });

    private static CdcChangeRecord UpsertRecord(object payload, int id)
    {
        var after = JsonDocument.Parse(JsonSerializer.Serialize(payload)).RootElement;
        return new CdcChangeRecord("cdc.public.GroupJoinRequests", BuildKey(id), after, IsDeleted: false, SourceTimestampMs: 1791431924810, Offset: 0);
    }

    private static readonly Guid Uuid = Guid.Parse("52b2b855-f914-4b9e-b4ac-548d34bf639e");

    private static object Answered(int id) => new
    {
        GroupJoinRequestId = id,
        GroupJoinRequestUuid = Uuid,
        GroupId = 2,
        PersonId = 5,
        Status = 3,
        AnsweredByPersonId = 9,
        AnsweredOn = "2026-10-08T04:00:00.000Z",
        ExpiresOn = "2026-10-22T03:58:44.798Z",
        InsertedOn = "2026-10-08T03:58:44.798Z",
        UpdatedOn = "2026-10-08T04:00:00.000Z",
        __source_ts_ms = 1791431924810L,
        __op = "u"
    };

    private static object Unanswered(int id) => new
    {
        GroupJoinRequestId = id,
        GroupJoinRequestUuid = Uuid,
        GroupId = 2,
        PersonId = 5,
        Status = 0,
        AnsweredByPersonId = (int?)null,
        AnsweredOn = (string?)null,
        ExpiresOn = "2026-10-22T03:58:44.798Z",
        InsertedOn = "2026-10-08T03:58:44.798Z",
        UpdatedOn = (string?)null,
        __source_ts_ms = 1791431924810L,
        __op = "c"
    };

    [Test]
    public async Task ApplyAsync_Should_UpsertEveryColumn_When_TheRecordIsNotDeleted()
    {
        await CreateHandler().ApplyAsync(UpsertRecord(Answered(7), 7), CancellationToken.None);

        _cqlExecutorMock.Verify(e => e.ExecuteAsync(QueryGroupJoinRequests.UpsertCql, It.IsAny<Action<Dictionary<string, object>>>()), Times.Once);
        _cqlExecutorMock.Verify(e => e.ExecuteAsync(QueryGroupJoinRequests.DeleteCql, It.IsAny<Action<Dictionary<string, object>>>()), Times.Never);
        _captured.ShouldNotBeNull();
        _captured!["@GROUPJOINREQUESTID"].ShouldBe(7);
        _captured["@GROUPJOINREQUESTUUID"].ShouldBe(Uuid);
        _captured["@GROUPID"].ShouldBe(2);
        _captured["@PERSONID"].ShouldBe(5);
        _captured["@STATUS"].ShouldBe(3);
        _captured["@ANSWEREDBYPERSONID"].ShouldBe(9);
        _captured["@ANSWEREDON"].ShouldBe(DateTimeOffset.Parse("2026-10-08T04:00:00.000Z"));
        _captured["@INSERTEDON"].ShouldBe(DateTimeOffset.Parse("2026-10-08T03:58:44.798Z"));
        _captured["@EXPIRESON"].ShouldBe(DateTimeOffset.Parse("2026-10-22T03:58:44.798Z"));
    }

    [Test]
    public async Task ApplyAsync_Should_MirrorARecordFromBeforeRequestsExpired_WithoutExpiry()
    {
        // Written before SCHEMA-39: no ExpiresOn at all. Replayed, it must not stop the Worker.
        var before = new
        {
            GroupJoinRequestId = 4,
            GroupJoinRequestUuid = Uuid,
            GroupId = 2,
            PersonId = 5,
            Status = 0,
            AnsweredByPersonId = (int?)null,
            AnsweredOn = (string?)null,
            InsertedOn = "2026-10-08T03:58:44.798Z",
            UpdatedOn = (string?)null,
            __source_ts_ms = 1791431924810L,
            __op = "c"
        };

        await CreateHandler().ApplyAsync(UpsertRecord(before, 4), CancellationToken.None);

        _captured.ShouldNotBeNull();
        _captured!["@EXPIRESON"].ShouldBeNull();
    }

    [Test]
    public async Task ApplyAsync_Should_PassNulls_When_TheNullableColumnsAreJsonNull()
    {
        await CreateHandler().ApplyAsync(UpsertRecord(Unanswered(8), 8), CancellationToken.None);

        _captured.ShouldNotBeNull();
        _captured!["@ANSWEREDBYPERSONID"].ShouldBeNull();
        _captured["@ANSWEREDON"].ShouldBeNull();
        _captured["@UPDATEDON"].ShouldBeNull();
    }

    [Test]
    public async Task ApplyAsync_Should_Delete_When_TheRecordIsDeleted()
    {
        var record = new CdcChangeRecord("cdc.public.GroupJoinRequests", BuildKey(3), After: null, IsDeleted: true, SourceTimestampMs: 0, Offset: 1);

        await CreateHandler().ApplyAsync(record, CancellationToken.None);

        _cqlExecutorMock.Verify(e => e.ExecuteAsync(QueryGroupJoinRequests.UpsertCql, It.IsAny<Action<Dictionary<string, object>>>()), Times.Never);
        _cqlExecutorMock.Verify(e => e.ExecuteAsync(QueryGroupJoinRequests.DeleteCql, It.IsAny<Action<Dictionary<string, object>>>()), Times.Once);
        _captured!["@GROUPJOINREQUESTID"].ShouldBe(3);
    }

    [Test]
    public async Task ApplyAsync_Should_Rethrow_When_ScyllaFails()
    {
        _cqlExecutorMock
            .Setup(e => e.ExecuteAsync(QueryGroupJoinRequests.UpsertCql, It.IsAny<Action<Dictionary<string, object>>>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        await Should.ThrowAsync<InvalidOperationException>(() => CreateHandler().ApplyAsync(UpsertRecord(Answered(1), 1), CancellationToken.None));
    }

    [Test]
    public void Topics_Should_Contain_Only_CdcPublicGroupJoinRequests()
    {
        CreateHandler().Topics.ShouldBe(["cdc.public.GroupJoinRequests"]);
    }
}
